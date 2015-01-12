using BeatlesBlog.SimConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSActiveFires {
    class SimConnectHelpers {
        public static bool IsLocalRunning {
            get { return LookupDefaultPortNumber("SimConnect_Port_IPv4") != 0 || LookupDefaultPortNumber("SimConnect_Port_IPv6") != 0; }
        }

        public static int LookupDefaultPortNumber(string strValueName) {
            string[] simulators = {
                                      @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft Games\Flight Simulator",
                                      @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft ESP",
                                      @"HKEY_CURRENT_USER\Software\LockheedMartin\Prepar3D",
                                      @"HKEY_CURRENT_USER\Software\Lockheed Martin\Prepar3D v2",
                                      @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft Games\Flight Simulator - Steam Edition"
                                  };
            foreach (string sim in simulators) {
                string value = (string)Microsoft.Win32.Registry.GetValue(sim, strValueName, null);
                if (!string.IsNullOrEmpty(value)) {
                    int port = int.Parse(value);
                    if (port != 0) { return port; }
                }
            }
            return 0;
        }
    }

    class SimConnectInstance : NotifyPropertyChanged {
        private SimConnect sc = null;
        private HashSet<SimObject> AllObjects;
        private Dictionary<uint, SimObject> ObjectsInSimulation;
        private uint ObjectIndex;
        private Log log;

        private const int maximumRequests = 10000;
        private const int placementRadiusMeters = 20000;
        private const string appName = "FS Active Fires";
        private const double RADIUS_EARTH_M = 6378137; // for use with spherical law of cosines

        private bool _isConnected;
        public bool IsConnected { get { return _isConnected; } private set { SetProperty(ref _isConnected, value); } }

        public int CreatedSimObjectsCount { get { return ObjectsInSimulation.Count; } } // make sure to manually trigger OnPropertyChanged

        public SimConnectInstance() {
            log = Log.Instance;
            sc = new SimConnect(null);

            sc.OnRecvOpen += sc_OnRecvOpen;
            sc.OnRecvException += sc_OnRecvException;
            sc.OnRecvQuit += sc_OnRecvQuit;

            sc.OnRecvEventObjectAddremove += sc_OnRecvEventObjectAddremove;
            sc.OnRecvAssignedObjectId += sc_OnRecvAssignedObjectId;
            sc.OnRecvSimobjectData += sc_OnRecvSimobjectData;

            AllObjects = new HashSet<SimObject>();
            ObjectsInSimulation = new Dictionary<uint, SimObject>();
        }

        public void Connect() {
            if (SimConnectHelpers.IsLocalRunning) {
                try {
                    log.Info("Opening SimConnect connection.");
                    sc.Open(appName);
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(string.Format("Local connection failed.\r\n{0}", ex.ToString()));
                    try {
                        bool ipv6support = System.Net.Sockets.Socket.OSSupportsIPv6;
                        log.Info("Opening SimConnect connection " + (ipv6support ? "(IPv6)." : "(IPv4)."));
                        int scPort = ipv6support ? SimConnectHelpers.LookupDefaultPortNumber("SimConnect_Port_IPv6") : SimConnectHelpers.LookupDefaultPortNumber("SimConnect_Port_IPv4");
                        if (scPort == 0) { throw new SimConnect.SimConnectException("Invalid port."); }
                        sc.Open(appName, null, scPort, ipv6support);
                    }
                    catch (SimConnect.SimConnectException innerEx) {
                        log.Error(string.Format("Local connection failed.\r\n{0}", innerEx.ToString()));
                    }
                }
            }
            else {
                log.Warning("Flight Simulator must be running in order to connect to SimConnect.");
            }
        }

        public void Disconnect() {
            log.Info("Closing SimConnect connection.");
            sc.Close();
            IsConnected = false;
            AllObjects.Clear();
            ObjectsInSimulation.Clear();
            OnPropertyChanged("CreatedSimObjectsCount");
        }

        void sc_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data) {
            log.Info(string.Format("Connected to {0}\r\n    Simulator Version:\t{1}.{2}.{3}.{4}\r\n    SimConnect Version:\t{5}.{6}.{7}.{8}",
                data.szApplicationName, data.dwApplicationVersionMajor, data.dwApplicationVersionMinor, data.dwApplicationBuildMajor, data.dwApplicationBuildMinor,
                data.dwSimConnectVersionMajor, data.dwSimConnectVersionMinor, data.dwSimConnectBuildMajor, data.dwSimConnectBuildMinor));

            IsConnected = true;

#if DEBUG
            sc.SubscribeToSystemEvent(Events.AddObject, "ObjectAdded");
#endif
            sc.SubscribeToSystemEvent(Events.RemoveObject, "ObjectRemoved");

            sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 5.0f, Requests.DisplayText, appName + " is connected to " + Encoding.UTF8.GetString(Encoding.Default.GetBytes(data.szApplicationName)));

            //sc.RequestDataOnUserSimObject(Requests.UserPosition, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 10, typeof(LatLon)); // Request user position every 10 seconds
            sc.RequestDataOnUserSimObject(Requests.UserPosition, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, typeof(LatLon));
        }

        void sc_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data) {
            log.Warning(string.Format("OnRecvException: {0} ({1}) {2} {3}", data.dwException.ToString(), Enum.GetName(typeof(SIMCONNECT_EXCEPTION), data.dwException), data.dwSendID.ToString(), data.dwIndex.ToString()));
            sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 10.0f, Requests.DisplayText, string.Format("{0} SimConnect Exception: {1} ({2})", appName, data.dwException.ToString(), Enum.GetName(typeof(SIMCONNECT_EXCEPTION), data.dwException)));
        }

        void sc_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data) {
            log.Info("OnRecvQuit - Simulator has closed.");
            Disconnect();
        }

        void sc_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data) {
            if ((Requests)data.dwRequestID == Requests.UserPosition) {
                LatLon userPos = (LatLon)data.dwData;
                CreateNearbyObjects(new Coordinate(userPos.Latitude, userPos.Longitude), placementRadiusMeters);
                RemoveFarAwayObjects(new Coordinate(userPos.Latitude, userPos.Longitude), placementRadiusMeters);
            }
        }

        void sc_OnRecvEventObjectAddremove(SimConnect sender, SIMCONNECT_RECV_EVENT_OBJECT_ADDREMOVE data) {
            switch ((Events)data.uEventID) {
#if DEBUG
                case Events.AddObject:
                    log.Info(string.Format("AddObject: {0} SIMCONNECT_SIMOBJECT_TYPE: {1}", data.dwData, Enum.GetName(typeof(SIMCONNECT_SIMOBJECT_TYPE), data.eObjType)));
                    break;
#endif
                case Events.RemoveObject:
                    if (ObjectsInSimulation.Values.Any(x => x.ObjectID == data.dwData)) {
                        log.Info(string.Format("RemoveObject: {0} (created by client)", data.dwData));
                        ObjectsInSimulation.Remove(ObjectsInSimulation.Single(x => x.Value.ObjectID == data.dwData).Key);
                        OnPropertyChanged("CreatedSimObjectsCount");
                    }
                    break;
            }
        }

        void sc_OnRecvAssignedObjectId(SimConnect sender, SIMCONNECT_RECV_ASSIGNED_OBJECT_ID data) {
            if (data.dwRequestID >= (uint)Requests.AICreateBase && data.dwRequestID < (uint)Requests.AICreateBase + maximumRequests) {
                ObjectsInSimulation[data.dwRequestID - (uint)Requests.AICreateBase].ObjectID = data.dwObjectID;
                log.Info("OnRecvAssignedObjectId (Requests.CreateAI): " + data.dwObjectID);
                OnPropertyChanged("CreatedSimObjectsCount");
                OnPropertyChanged("ObjectsCreated");
            }
        }

        // returns distance in meters
        private double Distance(Coordinate origin, Coordinate destination) {
            double lat1 = origin.Latitude;
            double lon1 = origin.Longitude;
            double lat2 = destination.Latitude;
            double lon2 = destination.Longitude;

            lat1 = (Math.PI / 180) * lat1;
            lon1 = (Math.PI / 180) * lon1;
            lat2 = (Math.PI / 180) * lat2;
            lon2 = (Math.PI / 180) * lon2;

            return Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon2 - lon1)) * RADIUS_EARTH_M;
        }

        private void CreateSimulatedObject(SimObject simObject, int hdg) {
            log.Info(string.Format("Adding {0} at {1}, {2}", simObject.Title, simObject.Location.Latitude, simObject.Location.Longitude));
            ObjectsInSimulation.Add(++ObjectIndex, simObject);
            sc.AICreateSimulatedObject(simObject.Title, new SIMCONNECT_DATA_INITPOSITION(simObject.Location.Latitude, simObject.Location.Longitude, 0, 0, 0, hdg, true, 0), (Requests)((int)Requests.AICreateBase + ObjectIndex));
        }

        public void AddLocations(string title, IEnumerable<Hotspot> locations) {
            log.Info(string.Format("Adding {0} at {1} total location(s).", title, locations.Count()));
            foreach (var hotspot in locations) {
                AllObjects.Add(new SimObject(title, hotspot.Latitude, hotspot.Longitude));
            }
        }

        private void CreateNearbyObjects(Coordinate userLocation, int radius) {
            int minLat = (int)userLocation.Latitude - 2;
            int maxLat = (int)userLocation.Latitude + 2;
            int minLon = (int)userLocation.Longitude - 2;
            int maxLon = (int)userLocation.Longitude + 2;
            var nearbyLocations = AllObjects.Where(x => x.Location.Latitude > minLat && x.Location.Latitude < maxLat && x.Location.Longitude > minLon && x.Location.Longitude < maxLon);

            foreach (var simObject in nearbyLocations) {
                if (Distance(userLocation, simObject.Location) < radius) {
                    if (!ObjectsInSimulation.Values.Contains(simObject)) {
                        Random r = new Random();
                        int randHdg = r.Next(0, 360);
                        CreateSimulatedObject(simObject, randHdg);
                    }
                }
            }
        }

        private void RemoveFarAwayObjects(Coordinate userLocation, int radius) {
            foreach (var simObject in ObjectsInSimulation.Values) {
                if (Distance(userLocation, simObject.Location) > radius) {
                    sc.AIRemoveObject(simObject.ObjectID, Requests.RemoveAI);
                }
            }
        }

        public void RelocateUserRandomly() {
            if (AllObjects.Count > 0) {
                Random r = new Random();
                var location = AllObjects.ElementAt(r.Next(0, AllObjects.Count)).Location;
                log.Info(string.Format("Relocating user to {0}, {1}", location.Latitude, location.Longitude));
                sc.SetDataOnUserSimObject(new SIMCONNECT_DATA_INITPOSITION(location.Latitude, location.Longitude, 3000, 0, 0, 0, false, 0));
            }
            else {
                string message = appName + ": Unable to relocate user.  No fire locations found.";
                log.Warning(message);
                sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 5.0f, Requests.DisplayText, message);
            }
        }
    }
}