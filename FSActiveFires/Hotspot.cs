
namespace FSActiveFires {
    struct Hotspot {
        public double Latitude;
        public double Longitude;
        public int Confidence;

        public Hotspot(double lat, double lon, int confidence) {
            Latitude = lat;
            Longitude = lon;
            Confidence = confidence;
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            Hotspot comparisonObj = (Hotspot)obj;
            return (comparisonObj.Latitude == this.Latitude) && (comparisonObj.Longitude == this.Longitude);
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = hash * 23 + Latitude.GetHashCode();
            hash = hash * 23 + Longitude.GetHashCode();
            return hash;
        }
    }
}
