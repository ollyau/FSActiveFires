using BeatlesBlog.SimConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSActiveFires {
    struct Coordinate {
        public double Latitude;
        public double Longitude;

        public Coordinate(double lat, double lon) {
            Latitude = lat;
            Longitude = lon;
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            Coordinate comparisonObj = (Coordinate)obj;
            return (comparisonObj.Latitude == this.Latitude) && (comparisonObj.Longitude == this.Longitude);
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = hash * 23 + Latitude.GetHashCode();
            hash = hash * 23 + Longitude.GetHashCode();
            return hash;
        }
    }

    class SimConnectInstance : ViewModelBase {

        // Private fields

        private SimConnect sc = null;
        private Dictionary<Coordinate, string> AllObjects;
        private Dictionary<uint, Coordinate> ObjectsInSimulation;

        private const int placementRadiusKm = 20;
        private const string appName = "FS Active Fires";

        private string _textOutput;
        private bool _isConnected;
        private bool _isLogsChangedPropertyInViewModel;
        private bool _loggingEnabled;

        // Public properties

        public string TextOutput { get { return _textOutput; } set { SetField(ref _textOutput, value); IsLogsChangedPropertyInViewModel = true; } }
        public bool IsConnected { get { return _isConnected; } private set { SetField(ref _isConnected, value); } }
        public bool IsLogsChangedPropertyInViewModel { get { return _isLogsChangedPropertyInViewModel; } set { SetField(ref _isLogsChangedPropertyInViewModel, value); } }
        public bool LoggingEnabled { get { return _loggingEnabled; } private set { SetField(ref _loggingEnabled, value); } }

        public int CreatedSimObjectsCount { get { return ObjectsInSimulation.Count; } }                 // Make sure to trigger OnPropertyChanged manually
        public bool ObjectsCreated { get { return ObjectsInSimulation.Count > 0 ? true : false; } }     // Make sure to trigger OnPropertyChanged manually


        /// <summary>
        /// Default constructor.  Instantiates the class and hooks SimConnect event handlers.
        /// </summary>
        public SimConnectInstance() {
            // Instantiate the SimConnect class
            sc = new SimConnect(null);

            // Set logging enabled or disabled
#if DEBUG
            LoggingEnabled = true;
#else
            LoggingEnabled = false;
#endif

            // hook needed events
            sc.OnRecvOpen += new SimConnect.RecvOpenEventHandler(sc_OnRecvOpen);
            sc.OnRecvException += new SimConnect.RecvExceptionEventHandler(sc_OnRecvException);
            sc.OnRecvQuit += new SimConnect.RecvQuitEventHandler(sc_OnRecvQuit);

            sc.OnRecvEventObjectAddremove += new SimConnect.RecvEventObjectAddremoveEventHandler(sc_OnRecvEventObjectAddremove);
            sc.OnRecvAssignedObjectId += new SimConnect.RecvAssignedObjectIdEventHandler(sc_OnRecvAssignedObjectId);
            sc.OnRecvSimobjectData += sc_OnRecvSimobjectData;

            // Instantiate dictionaries
            AllObjects = new Dictionary<Coordinate, string>();
            ObjectsInSimulation = new Dictionary<uint, Coordinate>();

            // Give output
            AddOutput(appName + " by Orion Lyau\r\nVersion: " + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion + "\r\n");
        }

        ~SimConnectInstance() {
            sc = null;
            AllObjects = null;
            ObjectsInSimulation = null;
        }

        private void AddOutput(string text) {
            if (LoggingEnabled) {
                TextOutput += text + "\r\n";
            }
#if DEBUG
            Console.WriteLine(text);
#endif
        }

        /// <summary>
        /// Initialization method.  Connects to the simulator.
        /// </summary>
        public void Connect() {
            // null if p3d isn't installed
            string p3d = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\LockheedMartin\Prepar3D", "SetupPath", null);
            string p3d2 = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Lockheed Martin\Prepar3D v2", "SetupPath", null);

            // check for prepar3d because it messes up the IsLocalRunning return value
            if (p3d == null && p3d2 == null) {
                if (SimConnect.IsLocalRunning()) {
                    // allow attempt a local connection if one appears to be running
                    // make local pipe connection (default local mode)
                    try {
                        sc.Open(appName);
                    }
                    catch (SimConnect.SimConnectException) {
                        AddOutput("Local connection failed.");
                    }
                }
                else {
                    AddOutput("No local SimConnect instance available.");
                }
            }
            else {
                // p3d is installed; IsLocalRunning won't detect fsx
                // just try connecting
                try {
                    sc.Open(appName);
                }
                catch (SimConnect.SimConnectException) {
                    AddOutput("Local connection failed.");
                }
            }
        }

        /// <summary>
        /// Closes the SimConnect connection.
        /// </summary>
        public void Disconnect() {
            AddOutput("Disconnecting.");

            ObjectsInSimulation.Clear();
            sc.Close();
            IsConnected = false;
        }

        /// <summary>
        /// Callback for the SimConnect open event.  Writes information, maps key events, subscribes to AI add/remove events, and requests data on initially loaded AI.
        /// </summary>
        void sc_OnRecvOpen(BeatlesBlog.SimConnect.SimConnect sender, BeatlesBlog.SimConnect.SIMCONNECT_RECV_OPEN data) {
            // Write log info
            AddOutput("Connected to " + data.szApplicationName +
                "\r\n    Simulator Version:\t" + data.dwApplicationVersionMajor + "." + data.dwApplicationVersionMinor + "." + data.dwApplicationBuildMajor + "." + data.dwApplicationBuildMinor +
                "\r\n    SimConnect Version:\t" + data.dwSimConnectVersionMajor + "." + data.dwSimConnectVersionMinor + "." + data.dwSimConnectBuildMajor + "." + data.dwSimConnectBuildMinor +
                "\r\n");

            // Set variable
            IsConnected = true;

            // Subscribe to events
            sc.SubscribeToSystemEvent(Events.AddObject, "ObjectAdded");
            sc.SubscribeToSystemEvent(Events.RemoveObject, "ObjectRemoved");

            // alert user that it connected
            sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 5.0f, Requests.DisplayText, appName + " is connected to " + data.szApplicationName);

            // Request user position every 10 seconds
            //sc.RequestDataOnUserSimObject(Requests.UserPosition, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 10, typeof(LatLon));
            sc.RequestDataOnUserSimObject(Requests.UserPosition, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, typeof(LatLon));
        }

        /// <summary>
        /// Callback for SimConnect exceptions.
        /// </summary>
        void sc_OnRecvException(BeatlesBlog.SimConnect.SimConnect sender, BeatlesBlog.SimConnect.SIMCONNECT_RECV_EXCEPTION data) {
            AddOutput("OnRecvException: " + data.dwException.ToString() + " (" + Enum.GetName(typeof(SIMCONNECT_EXCEPTION), data.dwException) + ")" + "  " + data.dwSendID.ToString() + "  " + data.dwIndex.ToString());
            sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 10.0f, Requests.DisplayText, appName + " SimConnect Exception: " + data.dwException.ToString() + " (" + Enum.GetName(typeof(SIMCONNECT_EXCEPTION), data.dwException) + ")");
        }

        /// <summary>
        /// Callback for quit events.  This is when the simulator exits.
        /// </summary>
        void sc_OnRecvQuit(BeatlesBlog.SimConnect.SimConnect sender, BeatlesBlog.SimConnect.SIMCONNECT_RECV data) {
            AddOutput("OnRecvQuit\tSimulator has closed.");
            Disconnect();
        }

        /**
         * Custom functions
         */

        // radius of Earth in meters (used in distance function)
        private const double RADIUS_EARTH_M = 6378137;

        /// <summary>
        /// Calculates distance between two coordinates using spherical law of cosines.
        /// </summary>
        /// <param name="origin">Starting latitude longitude.</param>
        /// <param name="destination">Ending latitude longitude.</param>
        /// <returns>Distance in meters.</returns>
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

        /// <summary>
        /// Creates a SimObject using AICreateSimulatedObject.
        /// </summary>
        /// <param name="title">Title of SimObject.</param>
        /// <param name="lat">Latitude of SimObject (decimal degrees).</param>
        /// <param name="lon">Latitude of SimObject (decimal degrees).</param>
        /// <param name="hdg">Heading of SimObject (degrees).</param>
        private void CreateSimulatedObject(string title, double lat, double lon, double hdg) {
            // inform user
            string info = "Adding " + title + " at " + lat + ", " + lon;
            AddOutput(info);
            // create new object
            sc.AICreateSimulatedObject(title, new SIMCONNECT_DATA_INITPOSITION(lat, lon, 0, 0, 0, hdg, true, 0), Requests.CreateAI);
        }

        /// <summary>
        /// Each entry represents a potential SimObject to be added to the simulation.
        /// </summary>
        /// <param name="title">Title of SimObject.</param>
        /// <param name="locations">Location of SimObject.</param>
        public void AddLocations(string title, Coordinate[] locations) {
            // loop through all the coordinates in the array
            foreach (Coordinate c in locations) {
                // add it if it's new
                if (!AllObjects.Keys.Contains(c)) {
                    AllObjects.Add(c, title);
                }
            }
        }

        /// <summary>
        /// Creates SimObjects from array that are only nearby the specified location
        /// </summary>
        /// <param name="userLocation">Current latitude longitude of user.</param>
        /// <param name="radius">Furthest distance from user in kilometers.</param>
        private void CreateNearbyObjects(Coordinate userLocation, int radius) {
            // change input km to m
            radius *= 1000;

            // get semi nearby lat/lon bounds first
            int minLat = (int)userLocation.Latitude - 2;
            int maxLat = (int)userLocation.Latitude + 2;

            int minLon = (int)userLocation.Longitude - 2;
            int maxLon = (int)userLocation.Longitude + 2;

            // get list of coordinates within bounds
            Coordinate[] nearbyLocations = AllObjects.Keys.Where(x => x.Latitude > minLat &&
                                                                    x.Latitude < maxLat &&
                                                                    x.Longitude > minLon &&
                                                                    x.Longitude < maxLon).ToArray();

            // for each coordinate
            foreach (Coordinate c in nearbyLocations) {
                // if it's nearby
                if (Distance(userLocation, c) < radius) {
                    // get decimal places of real coord
                    string realCoordLat = c.Latitude.ToString();
                    string realCoordLon = c.Longitude.ToString();

                    int decimalPlacesLat = realCoordLat.Substring(realCoordLat.IndexOf('.') + 1, realCoordLat.Length - (realCoordLat.IndexOf('.') + 1)).Length;
                    int decimalPlacesLon = realCoordLon.Substring(realCoordLon.IndexOf('.') + 1, realCoordLon.Length - (realCoordLon.IndexOf('.') + 1)).Length;

                    // make decimal places of sim coords the same precision
                    Coordinate[] RoundedCoords = ObjectsInSimulation.Values.ToArray();
                    for (int i = 0; i < RoundedCoords.Length; i++) {
                        RoundedCoords[i].Latitude = Math.Round(RoundedCoords[i].Latitude, decimalPlacesLat, MidpointRounding.AwayFromZero);
                        RoundedCoords[i].Longitude = Math.Round(RoundedCoords[i].Longitude, decimalPlacesLon, MidpointRounding.AwayFromZero);
                    }

                    // if it's not created already
                    if (!RoundedCoords.Contains(c)) {
                        // randomize heading
                        Random r = new Random();
                        int randHdg = r.Next(0, 360);
                        // add object
                        CreateSimulatedObject(AllObjects[c], c.Latitude, c.Longitude, randHdg);
                    }
                }
            }
        }

        /// <summary>
        /// Removes objects created by the program that are further away from the userLocation param than the specified distance
        /// </summary>
        /// <param name="userLocation">Current latitude longitude of user.</param>
        /// <param name="radius">Furthest distance from user in kilometers.</param>
        private void RemoveFarAwayObjects(Coordinate userLocation, int radius) {
            // change input km to m
            radius *= 1000;

            // for each coordinate
            foreach (Coordinate c in ObjectsInSimulation.Values) {
                // if it's too far away
                Console.WriteLine(Distance(userLocation, c));
                if (Distance(userLocation, c) > radius) {
                    // remove it
                    sc.AIRemoveObject(ObjectsInSimulation.Single(x => x.Value.Equals(c)).Key, Requests.RemoveAI);
                }
            }
        }

        /// <summary>
        /// Creates SimObjects with given title at all locations within given coordinate array.
        /// This function is depreciated in favor of CreateNearbyObjects(Coordinate userLocation, int radius)
        /// </summary>
        /// <param name="title">Title of SimObject.</param>
        /// <param name="locations">Array of coordinates.</param>
        private void CreateAllObjects(string title, Coordinate[] locations) {
            foreach (Coordinate c in locations) {
                Random r = new Random();
                int randHdg = r.Next(0, 360);
                CreateSimulatedObject(title, c.Latitude, c.Longitude, randHdg);
            }
        }

        /// <summary>
        /// Removes all SimObjects created by the client that are currently in the simulation.
        /// This function is depreciated in favor of RemoveFarAwayObjects(Coordinate userLocation, int radius)
        /// </summary>
        private void RemoveAllObjects() {
            foreach (uint id in ObjectsInSimulation.Keys) {
                sc.AIRemoveObject(id, Requests.RemoveAI);
            }
        }

        /// <summary>
        /// Relocates the user SimObject to a random location given a coordinate array.
        /// </summary>
        /// <param name="locations">Array of coordinates.</param>
        public void RelocateUserRandomly(Coordinate[] locations) {
            if (locations.Length > 0) {
                Random r = new Random();
                int idx = r.Next(0, locations.Length);
                sc.SetDataOnUserSimObject(new SIMCONNECT_DATA_INITPOSITION(locations[idx].Latitude, locations[idx].Longitude, 3500, 0, 0, 0, false, 0));
            }
            else {
                sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 5.0f, Requests.DisplayText, appName + ": Unable to relocate user.  No fire locations found.");
            }
        }

        /**
         * SimConnect Callbacks
         */

        /// <summary>
        /// This runs when:
        ///     we ask for the lat/lon of an object we made
        ///     or the periodic request on user position
        /// </summary>
        void sc_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data) {
            // here's one of our object creation requests
            if ((Requests)data.dwRequestID == (Requests)((int)Requests.AIAddInfo + data.dwObjectID)) {
                LatLon objectPos = (LatLon)data.dwData;
                // set the coordinate to the appropriate object id in the dictionary
                ObjectsInSimulation[data.dwObjectID] = new Coordinate(objectPos.Latitude, objectPos.Longitude);
                return;
            }

            switch ((Requests)data.dwRequestID) {
                case Requests.UserPosition:
                    // this is where the user position gets sent periodically
                    LatLon userPos = (LatLon)data.dwData;
                    // add objects in vicinity
                    CreateNearbyObjects(new Coordinate(userPos.Latitude, userPos.Longitude), placementRadiusKm);
                    // remove far away objects
                    RemoveFarAwayObjects(new Coordinate(userPos.Latitude, userPos.Longitude), placementRadiusKm);
                    break;
            }
        }

        /// <summary>
        /// This runs every time there's a SimObject added to or removed from the simulation
        /// This is where we find out if an object we made was removed from the simulation
        /// </summary>
        void sc_OnRecvEventObjectAddremove(SimConnect sender, SIMCONNECT_RECV_EVENT_OBJECT_ADDREMOVE data) {
            switch ((Events)data.uEventID) {
                case Events.AddObject:
                    if (ObjectsInSimulation.Keys.Contains(data.dwData)) {
                        // if it's one we know we added, let the user know
                        AddOutput("AddObject:\t" + data.dwData + " (created by client) SIMCONNECT_SIMOBJECT_TYPE: " + Enum.GetName(typeof(SIMCONNECT_SIMOBJECT_TYPE), data.eObjType));
                    }
                    else {
                        // we haven't heard of it before, but let's tell the user anyways
                        AddOutput("AddObject:\t" + data.dwData + " (unknown) SIMCONNECT_SIMOBJECT_TYPE: " + Enum.GetName(typeof(SIMCONNECT_SIMOBJECT_TYPE), data.eObjType));
                    }
                    break;
                case Events.RemoveObject:
                    if (ObjectsInSimulation.Keys.Contains(data.dwData)) {
                        // if one we made is removed, let the user know
                        AddOutput("RemoveObject:\t" + data.dwData + " (created by client)");

                        // and remove it from the list
                        ObjectsInSimulation.Remove(data.dwData);

                        // and update the GUI
                        OnPropertyChanged("CreatedSimObjectsCount");
                        OnPropertyChanged("ObjectsCreated");
                    }
                    else {
                        // just inform user that something was removed
                        AddOutput("RemoveObject:\t" + data.dwData + " (unknown)");
                    }
                    break;
            }
        }

        /// <summary>
        /// This gets run after we create a new simulated object.
        /// </summary>
        void sc_OnRecvAssignedObjectId(BeatlesBlog.SimConnect.SimConnect sender, BeatlesBlog.SimConnect.SIMCONNECT_RECV_ASSIGNED_OBJECT_ID data) {
            switch ((Requests)data.dwRequestID) {
                case Requests.CreateAI:
                    // tell the user we made it
                    AddOutput("CreateAI:\t" + data.dwObjectID);

                    // add it to our list
                    ObjectsInSimulation.Add(data.dwObjectID, new Coordinate());

                    // request its latitude and longitude so we can add the value in our dictionary
                    sc.RequestDataOnSimObject(
                        (Requests)((int)Requests.AIAddInfo + data.dwObjectID),
                        data.dwObjectID,
                        SIMCONNECT_PERIOD.ONCE,
                        SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                        typeof(LatLon));

                    // update the UI
                    OnPropertyChanged("CreatedSimObjectsCount");
                    OnPropertyChanged("ObjectsCreated");
                    break;
                default:
                    // apparently we made something we don't know of?
                    AddOutput("OnRecvAssignedObjectId:\t" + data.dwObjectID + " (unknown request)");
                    break;
            }
        }
    }
}