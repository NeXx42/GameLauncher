namespace GameLibary.Source.Database.Tables
{
    public class dbo_Tag : DatabaseTable
    {
        public override string tableName => "Tag";

        public int TagId { get; set; }
        public string TagName { get; set; }
        public string TagHexColour { get; set; }

        public override Row[] GetRows() => new Row[]
        {
            new Row(){ name = nameof(TagId), isAutoIncrement = true, isNullable = false, type = DataType.INTEGER, isPrimaryKey = true },
            new Row(){ name = nameof(TagName), type = DataType.TEXT, isNullable = false },
            new Row(){ name = nameof(TagHexColour), type = DataType.TEXT }
        };
    }
}
