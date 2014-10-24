
namespace FSActiveFires {
    class SimObject {
        public string Title;
        public Coordinate Location;
        public uint ObjectID;

        public SimObject(string title, double latitude, double longitude) {
            Title = title;
            Location = new Coordinate(latitude, longitude);
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            SimObject comparisonObj = (SimObject)obj;
            return (comparisonObj.Location.Equals(this.Location)) && (comparisonObj.Title.Equals(this.Title));
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = hash * 23 + Location.GetHashCode();
            hash = hash * 23 + Title.GetHashCode();
            return hash;
        }
    }
}
