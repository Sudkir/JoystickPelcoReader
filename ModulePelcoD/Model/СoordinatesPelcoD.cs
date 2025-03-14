namespace ModulePelcoD.Model
{
    public class СoordinatesPelcoD
    {
        /// <summary>
        /// Left:[-64:0] Right:[0:64]
        /// </summary>
        public float X { get; set; } = 0;

        /// <summary>
        /// Down:[-64:0] Up:[0:64]
        /// </summary>
        public float Y { get; set; } = 0;

        /// <summary>
        /// Rotate -1,0,1
        /// </summary>
        public float Z { get; set; } = 0;
    }
}