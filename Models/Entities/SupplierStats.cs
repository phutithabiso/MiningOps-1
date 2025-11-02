using System.ComponentModel;

namespace MiningOps.Models.Entities
{
    public class SupplierStats : INotifyPropertyChanged
    {
        private string _companyName;
        private int _orderCount;
        private decimal _totalOrderValue;

        public string CompanyName
        {
            get => _companyName;
            set
            {
                _companyName = value;
                OnPropertyChanged(nameof(CompanyName));
            }
        }

        public int OrderCount
        {
            get => _orderCount;
            set
            {
                _orderCount = value;
                OnPropertyChanged(nameof(OrderCount));
            }
        }

        public decimal TotalOrderValue
        {
            get => _totalOrderValue;
            set
            {
                _totalOrderValue = value;
                OnPropertyChanged(nameof(TotalOrderValue));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}