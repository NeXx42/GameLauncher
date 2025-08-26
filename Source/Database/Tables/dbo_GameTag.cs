using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibary.Source.Database.Tables
{
    public class dbo_GameTag : DatabaseTable
    {
        public override string tableName => "GameTag";

        public int GameId {  get; set; }
        public int TagId {  get; set; }

        public override Row[] GetRows() => new Row[]
        {
            new Row(){ name = nameof(GameId), type = DataType.INTEGER, isNullable = false},
            new Row(){ name = nameof(TagId), type = DataType.INTEGER, isNullable = false},
        };
    }
}
