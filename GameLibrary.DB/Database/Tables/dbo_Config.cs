namespace GameLibrary.DB.Tables
{
    public class dbo_Config : Database_Table
    {
        public override string tableName => "Config";


        public string key { get; set; }
        public string value { get; set; }

        public override Row[] GetRows() => new Row[] {
            new Row() {  name = nameof(key), type = DataType.TEXT, isPrimaryKey = true, isNullable = false },
            new Row() {  name = nameof(value), type = DataType.TEXT, isNullable = false },
        };
    }
}
