// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

using System.ComponentModel;

namespace LeeViewerUWP {
	public struct Coordinates : INotifyPropertyChanged {
		int i, j;

		public int I {
			get => i; set {
				i = value;
				OnPropertyChanged("I");
			}
		}
		public int J {
			get => j; set {
				j = value;
				OnPropertyChanged("J");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string PropertyName) {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
			}
		}

		public static bool operator==(Coordinates compare1, Coordinates compare2) {
			if (compare1.I == compare2.I && compare1.J == compare2.J) return true;
			return false;
		}

		public static bool operator !=(Coordinates compare1, Coordinates compare2) {
			if (compare1.I == compare2.I && compare1.J == compare2.J) return false;
			return true;
		}
	}
}
