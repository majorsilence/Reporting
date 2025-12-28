namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class BorderStyleType
    {
        private string _value;

        public BorderStyleType()
        {
            _value = "None";
        }
        public BorderStyleType(string value)
        {
            _value = value;
        }
        public static BorderStyleType None => new BorderStyleType("None");
        public static BorderStyleType Solid => new BorderStyleType("Solid");
        public static BorderStyleType Dashed => new BorderStyleType("Dashed");

        public static implicit operator string(BorderStyleType borderStyleType)
        {
            return borderStyleType.ToString();
        }

        public override string ToString()
        {
            return _value;
        }
    }
}