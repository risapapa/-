using System.Data;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//プロジェクト → 参照の追加 → アセンブリ → 拡張タブ
//Microsoft.Office.Interop.access.dao version 15.0.0.0
using Microsoft.Office.Interop.Access.Dao;

namespace ClassDao
{
    public partial class EditDaoDB2 {
        public static void QueryDef(string mDatabaseName, string QueryName, string SQLText) {
            //try {
                DBEngine dao = new DBEngine();
                Database mDb = dao.OpenDatabase(mDatabaseName, true, false);
                if (Exist(mDb, QueryName)) {
                    mDb.QueryDefs.Delete(QueryName);
                }    
                QueryDef qDef = mDb.CreateQueryDef(QueryName);
                qDef.SQL = SQLText;
                mDb.Close();
                mDb = null;
            //}
            //catch (Exception) {
            //}
        }
        private static Boolean Exist(Database mDb, string QueryName) {
            foreach (QueryDef qdf in mDb.QueryDefs) {
                if (qdf.Name == QueryName) {
                    //qdf.dele
                    return true;
                }
            }
            return false;
        }
        public static void DB最適化(string mDatabaseName) {
            string workAccdbFile = System.IO.Path.GetDirectoryName(mDatabaseName);
            if (!workAccdbFile.EndsWith(@"\")) {
                workAccdbFile += @"\";
            }
            workAccdbFile += "workAccdbFile.accdb";
            Console.WriteLine(workAccdbFile);
            if (File.Exists(workAccdbFile)) {
                File.Delete(workAccdbFile);
            }
            DBEngine dao = new DBEngine();
            dao.CompactDatabase(mDatabaseName, workAccdbFile);
            File.Delete(mDatabaseName);
            File.Move(workAccdbFile, mDatabaseName);
        }
    }
}
