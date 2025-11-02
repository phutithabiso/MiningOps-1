using MiningOps.Models.Entities;
using MiningOps.Services;
using MiningOps.Utilities;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System;

namespace MiningOps.ViewModels.Dialogs
{
    public class SupplierDialogViewModel : BaseViewModel
    {
        private Supplier _supplier;
        private List<RegisterMining> _availableUsers;
        private RegisterMining _selectedUser;
        private bool _isEditMode;
        private readonly UserService _userService;

        public SupplierDialogViewModel(UserService userService)
        {
            _userService = userService;
            _supplier = new Supplier();
            _isEditMode = false;

            InitializeCommands();
            LoadAvailableUsers();
        }

        public SupplierDialogViewModel(UserService userService, Supplier existingSupplier)
        {
            /*var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();*/

            _userService = userService;
            _supplier = existingSupplier;
            _isEditMode = true;

            InitializeCommands();
            LoadAvailableUsers();
        }

        public Supplier Supplier
        {
            get => _supplier;
            set
            {
                if (SetProperty(ref _supplier, value))
                {
                    // Update selected user when supplier changes
                    UpdateSelectedUser();
                }
            }
        }

        public List<RegisterMining> AvailableUsers
        {
            get => _availableUsers;
            set
            {
                if (SetProperty(ref _availableUsers, value))
                {
                    // Update selected user when available users load
                    UpdateSelectedUser();
                }
            }
        }

        public RegisterMining SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    // Update supplier AccId when user is selected
                    if (_selectedUser != null)
                    {
                        Supplier.AccId = _selectedUser.AccId;
                    }
                    else
                    {
                        Supplier.AccId = 0;
                    }
                }
            }
        }

        public string DialogTitle => _isEditMode ? "Edit Supplier" : "Add New Supplier";

        public ICommand SaveCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(() => { }, CanSave);
        }

        private async void LoadAvailableUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                AvailableUsers = users.ToList();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading users: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void UpdateSelectedUser()
        {
            if (_supplier?.AccId > 0 && AvailableUsers != null)
            {
                SelectedUser = AvailableUsers.FirstOrDefault(u => u.AccId == _supplier.AccId);
            }
            else
            {
                SelectedUser = null;
            }
        }

        public bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Supplier.CompanyName) &&
                   !string.IsNullOrWhiteSpace(Supplier.ContactPerson) &&
                   Supplier.AccId > 0; // Ensure a user is selected
        }
    }
}