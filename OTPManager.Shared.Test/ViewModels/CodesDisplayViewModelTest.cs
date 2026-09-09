using Moq;
using OTPManager.Shared.Models;
using OTPManager.Shared.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OTPManager.Shared.Test.ViewModels
{
    public class CodesDisplayViewModelTest : TestBase<CodesDisplayViewModel>
    {
        private static readonly IReadOnlyList<OTPGenerator> TestGenerators = Enumerable.Range(1, 3).Select(d => CreateOTPGenerator(d)).ToArray();

        protected override CodesDisplayViewModel GetTarget()
        {
            return new CodesDisplayViewModel(NavigatorMock.Object, DialogServiceMock.Object, DataStoreMock.Object, FileSystemMock.Object, BarcodeScannerMock.Object);
        }

        public CodesDisplayViewModelTest() : base()
        {
            DataStoreMock.Setup(d => d.GetAllAsync()).Returns(Task.FromResult(TestGenerators.ToList()));
        }

        [Fact]
        public async Task LoadingWorks()
        {
            Target.ViewAppearing();
            await Target.DataLoadedTCS.Task;

            Assert.True(Target.GeneratorsAvailable);

            Assert.Equal(TestGenerators.Count, Target.Items.Count);
            Assert.Equal(TestGenerators, Target.Items.Select(d=>d.Generator).ToArray());
        }

        [Fact]
        public void ManuallyCreatingEntryWorks()
        {
            Target.CreateEntryManual.Execute(null);

            NavigatorMock.Verify(d => d.Navigate<AddGeneratorViewModel>(null, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task SelectionWorks()
        {
            Target.ViewAppearing();
            await Target.DataLoadedTCS.Task;

            var selectedItem = Target.Items.First();

            Target.ItemClicked.Execute(selectedItem);
            NavigatorMock.Verify(d => d.Navigate<DisplayGeneratorViewModel, OTPGenerator>(selectedItem.Generator, null, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task FilteringByLabelWorks()
        {
            Target.ViewAppearing();
            await Target.DataLoadedTCS.Task;

            Target.SearchText = "Label 2";

            Assert.Single(Target.Items);
            Assert.Equal("Label 2", Target.Items.First().Label);
            Assert.False(Target.NoSearchResults);
            Assert.True(Target.IsSearchActive);
            Assert.True(Target.HasGenerators);
        }

        [Fact]
        public async Task FilteringByIssuerWorks()
        {
            Target.ViewAppearing();
            await Target.DataLoadedTCS.Task;

            Target.SearchText = "Issuer 3";

            Assert.Single(Target.Items);
            Assert.Equal("Issuer 3", Target.Items.First().Issuer);
            Assert.False(Target.NoSearchResults);
            Assert.True(Target.IsSearchActive);
        }

        [Fact]
        public async Task FilteringIsCaseInsensitive()
        {
            Target.ViewAppearing();
            await Target.DataLoadedTCS.Task;

            Target.SearchText = "label 1";

            Assert.Single(Target.Items);
            Assert.Equal("Label 1", Target.Items.First().Label);
        }

        [Fact]
        public async Task ClearingSearchRestoresAllItems()
        {
            Target.ViewAppearing();
            await Target.DataLoadedTCS.Task;

            Target.SearchText = "Label 1";
            Assert.Single(Target.Items);

            Target.ClearSearch.Execute(null);

            Assert.Equal(string.Empty, Target.SearchText);
            Assert.False(Target.IsSearchActive);
            Assert.Equal(TestGenerators.Count, Target.Items.Count);
            Assert.False(Target.NoSearchResults);
        }

        [Fact]
        public async Task SearchingWithNoResultsUpdatesProperties()
        {
            Target.ViewAppearing();
            await Target.DataLoadedTCS.Task;

            Target.SearchText = "NonExistingQuery123";

            Assert.Empty(Target.Items);
            Assert.False(Target.GeneratorsAvailable);
            Assert.True(Target.HasGenerators);
            Assert.True(Target.NoSearchResults);
            Assert.True(Target.IsSearchActive);
        }
    }
}
