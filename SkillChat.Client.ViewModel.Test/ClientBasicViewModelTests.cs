#nullable enable
using ReactiveUI;
using SkillChat.Client.Notification.ViewModels;
using SkillChat.Client.ViewModel;
using SkillChat.Client.ViewModel.Test.TestInfrastructure;

namespace SkillChat.Client.ViewModel.Test;

public class ClientBasicViewModelTests
{
    [Test]
    public async Task CurrentUserViewModel_TracksPasswordStateAndDisplayName()
    {
        ResetClientState();
        var user = new CurrentUserViewModel
        {
            Login = "login",
            UserName = "Display",
        };

        user.Password = "secret";

        using var _ = Assert.Multiple();
        await Assert.That(user.IsPassword).IsTrue();
        await Assert.That(user.DisplayName).IsEqualTo("Display");

        user.Password = string.Empty;

        await Assert.That(user.IsPassword).IsFalse();
        await Assert.That(user.ErrorMessageLoginPage).IsNotNull();
    }

    [Test]
    public async Task RegisterUserViewModel_Clear_ResetsFields()
    {
        ResetClientState();
        var viewModel = new RegisterUserViewModel
        {
            Login = "login",
            Password = "password",
            UserName = "name",
            Consent = true,
        };

        viewModel.Clear();

        using var _ = Assert.Multiple();
        await Assert.That(viewModel.Login).IsEqualTo(string.Empty);
        await Assert.That(viewModel.Password).IsEqualTo(string.Empty);
        await Assert.That(viewModel.UserName).IsEqualTo(string.Empty);
        await Assert.That(viewModel.Consent).IsFalse();
    }

    [Test]
    public async Task ErrorMessageViewModel_MapsKnownCodesAndCanReset()
    {
        ResetClientState();
        var viewModel = new ErrorMessageViewModel();

        viewModel.GetErrorMessage(ErrorMessageViewModel.ErrorAuthentication);
        await Assert.That(viewModel.ErrorMsg).IsEqualTo("Неверные Логин и/или Пароль");

        viewModel.GetErrorMessage("custom");
        await Assert.That(viewModel.ErrorMsg).IsEqualTo("custom");

        viewModel.ResetDisplayErrorMessage();
        await Assert.That(viewModel.ErrorMsg).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ConfirmationViewModel_OpenAndClose_UpdatesState()
    {
        ResetClientState();
        var viewModel = new ConfirmationViewModel();
        var command = ReactiveCommand.Create(() => { });

        viewModel.Open(command, "Подтвердить?", "Ок");

        using var _ = Assert.Multiple();
        await Assert.That(viewModel.IsOpened).IsTrue();
        await Assert.That(viewModel.ConfirmationQuestion).IsEqualTo("Подтвердить?");
        await Assert.That(viewModel.ButtonName).IsEqualTo("Ок");
        await Assert.That(viewModel.ConfirmSelectionCommand).IsSameReferenceAs(command);

        viewModel.Close();
        await Assert.That(viewModel.IsOpened).IsFalse();
    }

    [Test]
    public async Task Helpers_NameOrLogin_ReturnsDisplayNameOrLogin()
    {
        ResetClientState();
        await Assert.That(SkillChat.Client.ViewModel.Helpers.Helpers.NameOrLogin(" Display ", "login"))
            .IsEqualTo(" Display ");
        await Assert.That(SkillChat.Client.ViewModel.Helpers.Helpers.NameOrLogin(" ", "login"))
            .IsEqualTo("login");
    }

    [Test]
    public async Task NotifyViewModel_StoresProvidedText()
    {
        ResetClientState();
        var window = new TestNotifyWindow();
        var viewModel = new NotifyViewModel("User", "Text", window);

        using var _ = Assert.Multiple();
        await Assert.That(viewModel.UserLogin).IsEqualTo("User");
        await Assert.That(viewModel.Text).IsEqualTo("Text");
    }

    private static void ResetClientState()
    {
        TestHelpers.ResetLocator();
        TestHelpers.ResetNotificationManager();
    }
}
