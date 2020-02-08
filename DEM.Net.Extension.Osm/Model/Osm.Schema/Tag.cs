namespace DEM.Net.Extension.Osm.Schema
{
    public class Tag
    {
        public long Typ { get; set; }
        public string Value { get; set; }

        protected bool Equals(Tag other)
        {
            return Typ == other.Typ;
        }

        public override int GetHashCode()
        {
            return Typ.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Tag) obj);
        }
    }
}