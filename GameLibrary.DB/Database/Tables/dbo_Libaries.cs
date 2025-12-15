namespace GameLibrary.DB.Tables
{
    public class dbo_Libraries : Database_Table
    {
        public override string tableName => "Libraries";

        public int libaryId { get; set; }
        public required string rootPath { get; set; }


        public override Row[] GetRows() => new[]
        {
            new Row(){ name = nameof(libaryId), type = DataType.INTEGER, isNullable = false, isAutoIncrement = true, isPrimaryKey = true },
            new Row(){ name = nameof(rootPath), type = DataType.TEXT, isNullable = false},
        };
    }
}
