namespace HierarchyGrid.Definitions
{
    public enum Qualification
    {
        Unset,
        Empty,
        Normal,
        Error,
        Warning,
        Remark,
        Hovered
    }

    public class InputSet
    {
        public object Input { get; set; }
        public Qualification Qualifier { get; set; }
    }

    public class ResultSet
    {
        public string Result { get; set; }
        public Qualification Qualifier { get; set; }
    }
}