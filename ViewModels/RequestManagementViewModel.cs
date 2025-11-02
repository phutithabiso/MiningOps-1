using Microsoft.Extensions.Configuration;
using MiningOps.Dialogs;
using MiningOps.Models.Entities;
using MiningOps.Services;
using MiningOps.Utilities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MiningOps.ViewModels
{
    public class RequestManagementViewModel : BaseViewModel
    {
        private readonly RequestService _requestService;
        private ObservableCollection<MaterialRequest> _requests;
        private ObservableCollection<MaterialRequest> _urgentRequests;
        private MaterialRequest _selectedRequest;
        private string _searchText;
        private string _statusFilter = "All";

        // Dashboard Properties
        private int _pendingRequestsCount;
        private int _approvedRequestsCount;
        private int _rejectedRequestsCount;
        private string _approvalRate;
        private string _averageProcessingTime;
        private string _mostRequestedItem;
        private string _thisWeekRequests;

        public RequestManagementViewModel(RequestService requestService)
        {
            /*var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();*/

            _requestService = requestService ?? throw new ArgumentNullException(nameof(requestService));

            Requests = new ObservableCollection<MaterialRequest>();
            UrgentRequests = new ObservableCollection<MaterialRequest>();

            // Initialize commands
            AddRequestCommand = new RelayCommand(async () => await AddRequestAsync());
            EditRequestCommand = new RelayCommand<MaterialRequest>(async (request) => await EditRequestAsync(request));
            DeleteRequestCommand = new RelayCommand<MaterialRequest>(async (request) => await DeleteRequestAsync(request));
            ApproveRequestCommand = new RelayCommand<MaterialRequest>(async (request) => await ApproveRequestAsync(request));
            RejectRequestCommand = new RelayCommand<MaterialRequest>(async (request) => await RejectRequestAsync(request));
            RefreshCommand = new RelayCommand(async () => await LoadRequestsAsync());
            FilterCommand = new RelayCommand(async () => await FilterRequestsAsync());
            ExportRequestsReportCommand = new RelayCommand(async () => await ExportRequestsReportAsync());
            ExportPendingReportCommand = new RelayCommand(async () => await ExportPendingReportAsync());
            BulkApproveCommand = new RelayCommand(async () => await BulkApproveAsync());

            _ = LoadRequestsAsync();
        }

        public ObservableCollection<MaterialRequest> Requests
        {
            get => _requests;
            set => SetProperty(ref _requests, value);
        }

        public ObservableCollection<MaterialRequest> UrgentRequests
        {
            get => _urgentRequests;
            set => SetProperty(ref _urgentRequests, value);
        }

        public MaterialRequest SelectedRequest
        {
            get => _selectedRequest;
            set => SetProperty(ref _selectedRequest, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value);
        }

        // Dashboard Properties
        public int PendingRequestsCount
        {
            get => _pendingRequestsCount;
            set => SetProperty(ref _pendingRequestsCount, value);
        }

        public int ApprovedRequestsCount
        {
            get => _approvedRequestsCount;
            set => SetProperty(ref _approvedRequestsCount, value);
        }

        public int RejectedRequestsCount
        {
            get => _rejectedRequestsCount;
            set => SetProperty(ref _rejectedRequestsCount, value);
        }

        public string ApprovalRate
        {
            get => _approvalRate;
            set => SetProperty(ref _approvalRate, value);
        }

        public string AverageProcessingTime
        {
            get => _averageProcessingTime;
            set => SetProperty(ref _averageProcessingTime, value);
        }

        public string MostRequestedItem
        {
            get => _mostRequestedItem;
            set => SetProperty(ref _mostRequestedItem, value);
        }

        public string ThisWeekRequests
        {
            get => _thisWeekRequests;
            set => SetProperty(ref _thisWeekRequests, value);
        }

        public ICommand AddRequestCommand { get; }
        public ICommand EditRequestCommand { get; }
        public ICommand DeleteRequestCommand { get; }
        public ICommand ApproveRequestCommand { get; }
        public ICommand RejectRequestCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand ExportRequestsReportCommand { get; }
        public ICommand ExportPendingReportCommand { get; }
        public ICommand BulkApproveCommand { get; }

        private async Task LoadRequestsAsync()
        {
            try
            {
                var requests = await _requestService.GetAllRequestsAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Requests.Clear();
                    foreach (var request in requests)
                        Requests.Add(request);

                    // Calculate dashboard metrics
                    CalculateDashboardMetrics(requests);
                });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading requests: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateDashboardMetrics(List<MaterialRequest> requests)
        {
            PendingRequestsCount = requests.Count(r => r.Status == "Pending");
            ApprovedRequestsCount = requests.Count(r => r.Status == "Approved");
            RejectedRequestsCount = requests.Count(r => r.Status == "Rejected");

            // Calculate approval rate
            var totalProcessed = ApprovedRequestsCount + RejectedRequestsCount;
            ApprovalRate = totalProcessed > 0 ? $"{((double)ApprovedRequestsCount / totalProcessed * 100):0}%" : "0%";

            // Urgent requests (pending requests from last 7 days with high quantity)
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            UrgentRequests.Clear();
            foreach (var request in requests
                .Where(r => r.Status == "Pending" && r.RequestDate >= oneWeekAgo && r.Quantity > 10)
                .Take(5))
            {
                UrgentRequests.Add(request);
            }

            // Mock data for demonstration
            AverageProcessingTime = "1.2 days";

            // Most requested item
            var topItem = requests
                .GroupBy(r => r.ItemName)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            MostRequestedItem = topItem?.Key ?? "None";

            // This week's requests
            var thisWeekCount = requests.Count(r => r.RequestDate >= DateTime.UtcNow.AddDays(-7));
            ThisWeekRequests = $"{thisWeekCount} requests";
        }

        private async Task FilterRequestsAsync()
        {
            try
            {
                var allRequests = await _requestService.GetAllRequestsAsync();
                var filteredRequests = allRequests.Where(r =>
                    (StatusFilter == "All" || r.Status == StatusFilter) &&
                    (string.IsNullOrEmpty(SearchText) ||
                     r.ItemName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true)
                ).ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Requests.Clear();
                    foreach (var request in filteredRequests)
                        Requests.Add(request);
                });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error filtering requests: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddRequestAsync()
        {
            var dialog = new RequestDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _requestService.CreateRequestAsync(dialog.Request);
                    await LoadRequestsAsync();
                    MessageBox.Show("Request created successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error creating request: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task EditRequestAsync(MaterialRequest request)
        {
            if (request == null)
            {
                MessageBox.Show("Please select a request to edit", "No Request Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new RequestDialog(request);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _requestService.UpdateRequestAsync(dialog.Request);
                    await LoadRequestsAsync();
                    MessageBox.Show("Request updated successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error updating request: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task DeleteRequestAsync(MaterialRequest request)
        {
            if (request == null)
            {
                MessageBox.Show("Please select a request to delete", "No Request Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete request for '{request.ItemName}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _requestService.DeleteRequestAsync(request.MaterialRequestId);
                    await LoadRequestsAsync();
                    MessageBox.Show("Request deleted successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error deleting request: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ApproveRequestAsync(MaterialRequest request)
        {
            if (request == null)
            {
                MessageBox.Show("Please select a request to approve", "No Request Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to approve request for '{request.ItemName}'?",
                "Confirm Approval", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _requestService.ApproveRequestAsync(request.MaterialRequestId);
                    await LoadRequestsAsync();
                    MessageBox.Show("Request approved successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error approving request: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task RejectRequestAsync(MaterialRequest request)
        {
            if (request == null)
            {
                MessageBox.Show("Please select a request to reject", "No Request Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to reject request for '{request.ItemName}'?",
                "Confirm Rejection", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _requestService.RejectRequestAsync(request.MaterialRequestId);
                    await LoadRequestsAsync();
                    MessageBox.Show("Request rejected successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error rejecting request: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task BulkApproveAsync()
        {
            var pendingRequests = Requests.Where(r => r.Status == "Pending").ToList();
            if (!pendingRequests.Any())
            {
                MessageBox.Show("No pending requests to approve", "No Pending Requests",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to approve all {pendingRequests.Count} pending requests?",
                "Bulk Approval", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var request in pendingRequests)
                    {
                        await _requestService.ApproveRequestAsync(request.MaterialRequestId);
                    }
                    await LoadRequestsAsync();
                    MessageBox.Show($"{pendingRequests.Count} requests approved successfully", "Bulk Approval Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error during bulk approval: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExportRequestsReportAsync()
        {
            try
            {
                MessageBox.Show($"Request report generated for {Requests.Count} requests", "Report Generated",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportPendingReportAsync()
        {
            try
            {
                var pendingCount = Requests.Count(r => r.Status == "Pending");
                MessageBox.Show($"Pending requests report generated for {pendingCount} requests", "Report Generated",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}