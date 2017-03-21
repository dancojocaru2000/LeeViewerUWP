using System.ComponentModel;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LeeViewerUWP {
	public class OperationDisplay : INotifyPropertyChanged {
		private string text;
		private Symbol icon;

		public Symbol Icon {
			get => icon; set {
				icon = value;
				OnPropertyChanged("Icon");
			}
		}
		public string Text {
			get => text; set {
				text = value;
				OnPropertyChanged("Text");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string PropertyName) {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
			}
		}
	}
}
