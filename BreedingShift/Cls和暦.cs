using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FarPoint.Win.Spread;
using GrapeCity.Win.Editors;

namespace ClassLibrary1
{
    public partial class Cls和暦
    {
        public static void 和暦表示(object source) {
            if (source is GcDate) {
                GcDate e = (GcDate)source;
                e.Fields.Clear();
                e.DisplayFields.Clear();
                e.Fields.AddRange("gee/MM/dd");
                e.DisplayFields.AddRange("gee/MM/dd");
                e.DropDownCalendar.HeaderFormat = "ggg e 年 M 月";
                e.DropDownCalendar.UseHeaderFormat = true;
            }
            else if (source is FpSpread) {
                FarPoint.Win.Spread.CellType.DateTimeCellType dt2 = new FarPoint.Win.Spread.CellType.DateTimeCellType();
                dt2.Calendar = new System.Globalization.JapaneseCalendar();
                dt2.DateTimeFormat = FarPoint.Win.Spread.CellType.DateTimeFormat.UserDefined;
                dt2.UserDefinedFormat = "yy/MM/dd";
                FpSpread e = (FpSpread)source;
                for (int i = 0; i < e.Sheets[0].Columns.Count; i++) {
                    if (e.Sheets[0].Columns[i].CellType is FarPoint.Win.Spread.CellType.DateTimeCellType) {
                        e.Sheets[0].Columns[i].CellType = dt2;
                    }
                }
            }
        }
    }
}
