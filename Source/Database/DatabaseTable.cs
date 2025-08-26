﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GameLibary.Source.Database
{
    public abstract class DatabaseTable
    {
        public abstract string tableName { get; }

        public abstract Row[] GetRows();

        public string GenerateCreateCommand()
        {
            Row[] rows = GetRows();

            StringBuilder sql = new StringBuilder($"CREATE TABLE IF NOT EXISTS {tableName} ( ");
            
            for(int i = 0; i < rows.Length; i++)
            {
                Row r = rows[i];
                sql.Append($"{r.name} {r.type.ToString()}");

                switch (r.type)
                {
                    case DataType.INTEGER:
                        if(r.isPrimaryKey)
                            sql.Append(" PRIMARY KEY");

                        if (r.isAutoIncrement)
                            sql.Append(" AUTOINCREMENT");
                        break;
                }

                if (!r.isNullable)
                {
                    sql.Append(" NOT NULL");
                }

                if (i < rows.Length - 1)
                    sql.Append(",");
            }

            sql.Append(")");
            return sql.ToString();
        }


        public string GenerateInsertCommand()
        {
            StringBuilder sql = new StringBuilder($"INSERT INTO {tableName} (");
            StringBuilder vals = new StringBuilder("(");

            Row[] rows = GetRows();
            
            for(int i = 0; i < rows.Length; i++)
            {
                Row r = rows[i];

                if (!r.CanModify())
                    continue;

                string val = SerializeRow(r);

                sql.Append($"{r.name}");
                vals.Append(val);

                if (i < rows.Length - 1) 
                {
                    sql.Append(", ");
                    vals.Append(", ");
                }
            }

            sql.Append(") VALUES ");
            vals.Append(")");

            return sql.Append(vals).ToString();
        }


        public void Map(SQLiteDataReader reader)
        {
            Row[] rows = GetRows();

            foreach(Row r in rows)
            {
                object o = reader[r.name];

                if (o != null)
                {
                    DeserializeRow(r, o);
                }
            }
        }

        public string GenerateUpdateCommand()
        {
            StringBuilder sql = new StringBuilder($"UPDATE {tableName} SET ");

            Row[] rows = GetRows();

            for (int i = 0; i < rows.Length; i++)
            {
                Row r = rows[i];

                if (!r.CanModify())
                    continue;

                string val = SerializeRow(r);

                sql.Append($"{r.name} = {val}");

                if (i < rows.Length - 1)
                    sql.Append(", ");
            }

            return sql.ToString();
        }

        private void DeserializeRow(Row r, object o)
        {
            PropertyInfo prop = GetType().GetProperty(r.name);

            if(o == DBNull.Value)
            {
                prop.SetValue(this, null);
                return;
            }

            switch (r.type)
            {
                case DataType.INTEGER:
                    o = Convert.ToInt32(o);
                    break;

                case DataType.BIT:
                    o = Convert.ToInt64(o) == 1;
                    break;
            }

            prop.SetValue(this, o);
        }


        private string SerializeRow(Row r)
        {
            PropertyInfo? prop = GetType().GetProperty(r.name);
            string? val = prop!.GetValue(this)?.ToString();

            switch (r.type)
            {
                case DataType.TEXT:
                    val = val == null ? "NULL" : $"'{val}'";
                    break;

                case DataType.BIT:
                    val = string.Equals(val, "TRUE", StringComparison.InvariantCultureIgnoreCase) ? "1" : "0";
                    break;
            }

            return val;
        }


        public record Row
        {
            public string name;
            public DataType type = DataType.TEXT;
            public bool isNullable = true;
            public bool isPrimaryKey = false;
            public bool isAutoIncrement = false;

            public bool CanModify() => !isAutoIncrement;
        }

        public enum DataType
        {
            INTEGER,
            TEXT,
            BIT,
        }
    }
}
