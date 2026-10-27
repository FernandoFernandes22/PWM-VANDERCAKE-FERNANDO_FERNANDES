using Microsoft.Extensions.DependencyInjection;

namespace VanderCake.Mobile;

public partial class App : Application
{
	private readonly Services.ApiClient api;
	private readonly Services.DraftStore drafts;
	public App(Services.ApiClient api, Services.DraftStore drafts)
	{
		this.api = api;
		this.drafts = drafts;
		InitializeComponent();
		// As telas usam superfícies claras; os controles devem seguir a mesma paleta.
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new NavigationPage(new Views.LoginPage(api, drafts)));
	}
}
