using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;

namespace GameLibrary.AvaloniaUI.Controls.Pages;

public partial class Page_Login : UserControl
{
    private string? passwordHash;
    private Func<Task>? onSuccess;

    public Page_Login()
    {
        InitializeComponent();
        btn_Login.RegisterClick(AttemptToLoginAsync);
    }

    public void Enter(string passwordHash, Func<Task> onSuccess)
    {
        this.onSuccess = onSuccess;
        this.passwordHash = passwordHash;
    }

    private async Task AttemptToLoginAsync()
    {
        if (EncryptionHelper.TestPassword(inp_Password.Text, passwordHash))
            await onSuccess!();
    }
}