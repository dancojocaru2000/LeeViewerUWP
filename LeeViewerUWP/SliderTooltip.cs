using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace LeeViewerUWP {
	class SliderTooltip : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, string language) {
			int val = int.Parse((value as double?).Value.ToString());
			switch (val) {
			case 0:
				return "0 ms/cell (almost instantly)";
			case 1:
				return "10 ms/cell (small delay)";
			case 2:
				return "250 ms/cell (medium delay)";
			case 3:
				return "1000 ms/cell (big delay)";
			default:
				throw new NotImplementedException();
			}
		}
		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}
}
