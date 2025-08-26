﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibary.Source.Database.Tables
{
    public class dbo_Config : DatabaseTable
    {
        public override string tableName => "Config";


        public string key {  get; set; }
        public string value {  get; set; }

        public override Row[] GetRows() => new Row[] {
            new Row() {  name = nameof(key), type = DataType.TEXT, isPrimaryKey = true, isNullable = false },
            new Row() {  name = nameof(value), type = DataType.TEXT, isNullable = false },
        };
    }
}
