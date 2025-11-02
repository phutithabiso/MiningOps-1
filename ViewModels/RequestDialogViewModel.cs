using MiningOps.Models.Entities;
using MiningOps.Utilities;
using System;
using System.Windows.Input;

namespace MiningOps.ViewModels.Dialogs
{
    public class RequestDialogViewModel : BaseViewModel
    {
        private MaterialRequest _request;
        private bool _isEditMode;

        public RequestDialogViewModel()
        {
            _request = new MaterialRequest
            {
                Status = "Pending",
                RequestDate = DateTime.UtcNow
            };
            _isEditMode = false;

            InitializeCommands();
        }

        public RequestDialogViewModel(MaterialRequest existingRequest)
        {
            _request = existingRequest;
            _isEditMode = true;

            InitializeCommands();
        }

        public MaterialRequest Request
        {
            get => _request;
            set => SetProperty(ref _request, value);
        }

        public string DialogTitle => _isEditMode ? "Edit Material Request" : "Create New Material Request";

        public ICommand SaveCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(() => { }, CanSave);
        }

        public bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Request.ItemName) &&
                   Request.Quantity > 0;
        }
    }
}