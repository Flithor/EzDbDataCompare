﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using DataAccessLib.Common;
using DataAccessLib.Entities;

namespace DataAccessLib.DataAccessor
{
    public sealed class FoxProOleDb : DataAccessorBase<OleDbConnection>//←Set this DbConnection
    {
        public FoxProOleDb(DbConnectionStringInfo connStrInfo) : base(connStrInfo) { }

        #region ConnectionInfoSetting
        public override string ConnectionStringFormat => "Provider=vfpoledb;Data Source=\"{0}\";Collating Sequence={1};Mode=Read;";
        internal override string[] ConnectionStringFieldNames { get; } = { "Data Source", "\u200B\u200BTargetType" };
        internal override string[] ConnectionStringFieldDefaultValue { get; } = { null, "machine|general" };
        #endregion

        internal override string BuildConnectionString(params string[] fields)
        {
            var dataSource = fields[0];
            if (File.Exists(dataSource))
            {
                OneFileMode = true;
                fields[0] = Path.GetDirectoryName(dataSource);
                var re = string.Format(ConnectionStringFormat, fields);
                fields[0] = dataSource;
                return re;
            } //FileMode
            if (Directory.Exists(dataSource)) return string.Format(ConnectionStringFormat, fields);
            throw new FileNotFoundException("Not found this data source!"); //NotExit
        }

        private bool OneFileMode = false;

        #region Query
        public override IEnumerable<string> GetDataBaseTableNames()
        {
            var dataSource = DbConnectionStringInfo.ConnecgtionStringFieldValues[0];
            if (File.Exists(dataSource))
            {
                DbConnectionStringInfo.ConnecgtionStringFieldValues[0] = Path.GetDirectoryName(dataSource);
                return new[] { Path.GetFileNameWithoutExtension(dataSource) };
            } //FileMode
            else if (Directory.Exists(dataSource))
            {
                return Directory.GetFiles(dataSource, "*.dbf", SearchOption.AllDirectories).Select(fp =>
                {
                    var osb = new StringBuilder(1024);
                    Console.WriteLine(fp);
                    var re = RelativePath.PathRelativePathTo(osb, dataSource, FileAttributes.Directory, fp,
                         FileAttributes.Normal);
                    if (re)
                        return osb.ToString();
                    throw new Exception("Get relative path error!");
                }).ToArray();
            }//DirMode
            else { throw new FileNotFoundException("Not found this data source!"); } //NotExit
        }

        public override DataSet QueryTables(IEnumerable<string> tableNames, Action processCallBack = null)
        {
            throw new NotImplementedException();
        }
        public override DataTable QueryTable(string tableName)
        {
            if (OneFileMode) tableName = $"[{tableName}]";
            var dt = new DataTable();
            using (var conn = new OleDbConnection(ConnStr))
            {
                using (var adapter = new OleDbDataAdapter($"select * from {tableName}", conn))
                {
                    adapter.FillSchema(dt, SchemaType.Mapped);
                    dt.TableName = tableName;
                    adapter.Fill(dt);
                }
            }

            return dt;
        }
        #endregion
    }
}
