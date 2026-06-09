using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using EnumWrapper.SourceGenerators;
using EnumWrapper.SourceGenerators.Sample;

namespace EnumWrapper.SourceGenerators.Sample
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private OrderStatus _selectedStatus = OrderStatus.Pending;
        public OrderStatus SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (_selectedStatus != value)
                {
                    _selectedStatus = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedStatusWrapper));
                }
            }
        }

        // Expose wrapper for detail binding
        public OrderStatusWrapper SelectedStatusWrapper => OrderStatusWrapper.FromValue(SelectedStatus);

        private FileMode _selectedFileMode = FileMode.Open;
        public FileMode SelectedFileMode
        {
            get => _selectedFileMode;
            set
            {
                if (_selectedFileMode != value)
                {
                    _selectedFileMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedFileModeWrapper));
                }
            }
        }

        // Expose wrapper for detail binding
        public FileModeWrapper SelectedFileModeWrapper => FileModeWrapper.FromValue(SelectedFileMode);

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void SetDelivered_Click(object sender, RoutedEventArgs e)
        {
            // Programmatically set selection
            SelectedStatus = OrderStatus.Delivered;
        }

        private void SetOpenOrCreate_Click(object sender, RoutedEventArgs e)
        {
            // Programmatically set selection
            SelectedFileMode = FileMode.OpenOrCreate;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            SelectedStatus = OrderStatus.Pending;
            SelectedFileMode = FileMode.Open;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
