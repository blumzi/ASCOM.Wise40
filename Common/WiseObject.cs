namespace ASCOM.Wise40.Common
{
    public class WiseObject
    {
        protected string _name;

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }
    }
}