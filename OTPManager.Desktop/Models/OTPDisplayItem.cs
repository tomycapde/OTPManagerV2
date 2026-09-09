using OTPManager.Shared.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OTPManager.Desktop.Models
{
    public class OTPDisplayItem : INotifyPropertyChanged
    {
        public OTPGenerator Generator { get; }

        public string Issuer => string.IsNullOrWhiteSpace(Generator.Issuer) ? "" : Generator.Issuer;
        public string Label => string.IsNullOrWhiteSpace(Generator.Label) ? "Cuenta sin nombre" : Generator.Label;

        public string AccountName => !string.IsNullOrWhiteSpace(Generator.Label) 
            ? Generator.Label 
            : (!string.IsNullOrWhiteSpace(Generator.Issuer) ? Generator.Issuer : "Cuenta sin nombre");

        public List<string> Tags { get; }
        public bool HasTags { get; }
        public string TagsString { get; }
        public string SearchableText { get; }

        private string formattedOtp = "------";
        public string FormattedOTP
        {
            get => formattedOtp;
            private set
            {
                if (formattedOtp != value)
                {
                    formattedOtp = value;
                    OnPropertyChanged();
                }
            }
        }

        private string rawOtp = "";
        public string RawOTP
        {
            get => rawOtp;
            private set
            {
                if (rawOtp != value)
                {
                    rawOtp = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool isCopied = false;
        public bool IsCopied
        {
            get => isCopied;
            set
            {
                if (isCopied != value)
                {
                    isCopied = value;
                    OnPropertyChanged();
                }
            }
        }

        public OTPDisplayItem(OTPGenerator generator)
        {
            Generator = generator ?? throw new ArgumentNullException(nameof(generator));
            Tags = Generator.GetTagList();
            HasTags = Tags.Count > 0;
            TagsString = string.Join(" ", Tags);
            SearchableText = $"{AccountName} {Issuer} {TagsString}".ToLowerInvariant();
            UpdateOTP(DateTime.UtcNow);
        }

        public void UpdateOTP(DateTime time)
        {
            try
            {
                var code = Generator.GenerateOTP(time);
                RawOTP = code;
                if (code.Length == 6)
                {
                    FormattedOTP = $"{code.Substring(0, 3)} {code.Substring(3, 3)}";
                }
                else if (code.Length == 8)
                {
                    FormattedOTP = $"{code.Substring(0, 4)} {code.Substring(4, 4)}";
                }
                else
                {
                    FormattedOTP = code;
                }
            }
            catch
            {
                FormattedOTP = "ERROR";
                RawOTP = "";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
