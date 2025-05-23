﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using MudBlazor;
using SharedClasses;
using WebUI.Models;

namespace WebUI.Services;

public class UserService
{
    private readonly AuthenticationStateProvider _authProvider;
    private readonly DataState _dataState;
    private readonly NavigationManager _navManager;
    private readonly ISnackbar _snackbar;

    public UserService(
        AuthenticationStateProvider authProvider,
        NavigationManager navManager,
        DataState dataState,
        ISnackbar snackbar)
    {
        _authProvider = authProvider;
        _navManager = navManager;
        _snackbar = snackbar;
        _dataState = dataState;

        _authProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        OnAuthenticationStateChanged(_authProvider.GetAuthenticationStateAsync());
    }

    public bool IsUserAuthenticated { get; private set; }

    public event Func<Task>? OnAuthenticationStateChangedEvent; 

    public void BeginSignIn()
        => _navManager.NavigateToLogin("/authentication/login");

    public void BeginSignOut()
        => _navManager.NavigateToLogout("/authentication/logout");

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        var result = await task;
        IsUserAuthenticated = result.User.Identity?.IsAuthenticated ?? false;

        if (!IsUserAuthenticated)
            return;

        _snackbar.Add("You are now signed in and have access to GPT-4", Severity.Success);
        _snackbar.Add("Your GPT model has been set to GPT-4, you can change your model in Settings | API Key");
        _dataState.SelectedGptModel = AvailableGptModels.Gpt4;
        
        OnAuthenticationStateChangedEvent?.Invoke();
    }
}