namespace ModulePelcoD.Model
{
    internal class PortConverter
    {
        /// <summary>
        /// COM0-COM*
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Description { get; set; }

        public string? Manufacturer { get; set; }
        public string? DeviceID { get; set; }
        public string? Service { get; set; }
    }
}