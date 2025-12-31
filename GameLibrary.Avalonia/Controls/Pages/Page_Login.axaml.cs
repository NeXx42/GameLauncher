using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Controls.Pages;

public partial class Page_Login : UserControl
{
    private string? passwordHash;
    private Action? onSuccess;

    public Page_Login()
    {
        InitializeComponent();
        btn_Login.RegisterClick(AttemptToLogin);
    }

    public void Enter(string passwordHash, Action onSuccess)
    {
        this.onSuccess = onSuccess;
        this.passwordHash = passwordHash;
    }

    private void AttemptToLogin()
    {
        if (EncryptionHelper.TestPassword(inp_Password.Text, passwordHash))
            onSuccess?.Invoke();
    }
}