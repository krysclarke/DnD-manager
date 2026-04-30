using CommunityToolkit.Mvvm.ComponentModel;
using DnDManager.Services;

namespace DnDManager.ViewModels;

public partial class MainWindowViewModel : ObservableObject {
    private readonly ICampaignRepository _campaignRepository;
    public IThemeService ThemeService { get; }

    public EncounterTrackerViewModel EncounterTrackerVm { get; }
    public DiceRollerViewModel DiceRollerVm { get; }
    public MonsterManagerViewModel MonsterManagerVm { get; }
    public SettingsViewModel SettingsVm { get; }
    public WebServerViewModel? WebServerVm { get; set; }

    [ObservableProperty]
    private int _selectedTabIndex;

    // Window geometry (set by View before save, read by View on restore)
    public double? WindowX { get; set; }
    public double? WindowY { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public string? WindowState { get; set; }

    public MainWindowViewModel(
        ICampaignRepository campaignRepository,
        IDiceParser diceParser,
        IDiceRoller diceRoller,
        IEncounterService encounterService,
        IEncounterFileService encounterFileService,
        IBestiaryFileService bestiaryFileService,
        IOpen5eApiClient open5eApiClient,
        IThemeService themeService,
        ISpellDatabaseService spellDatabaseService) {
        _campaignRepository = campaignRepository;
        ThemeService = themeService;

        DiceRollerVm = new DiceRollerViewModel(diceParser, diceRoller);
        MonsterManagerVm = new MonsterManagerViewModel(
            bestiaryFileService, open5eApiClient, campaignRepository, spellDatabaseService);
        EncounterTrackerVm = new EncounterTrackerViewModel(
            DiceRollerVm, encounterService, encounterFileService, MonsterManagerVm, spellDatabaseService);
        SettingsVm = new SettingsViewModel(themeService, campaignRepository);
    }

    public async Task InitializeAsync() {
        await _campaignRepository.InitializeAsync(string.Empty);

        // Load custom themes before resolving saved theme ID
        await ThemeService.LoadCustomThemesAsync(_campaignRepository);

        // Load theme/scale settings before UI renders
        var themeId = await _campaignRepository.LoadSettingAsync("theme");
        var uiScaleStr = await _campaignRepository.LoadSettingAsync("uiScale");
        var webUiScaleStr = await _campaignRepository.LoadSettingAsync("webUiScale");

        var webThemeId = await _campaignRepository.LoadSettingAsync("webTheme");

        var uiScale = double.TryParse(uiScaleStr, out var s) ? s : 1.0;
        var webUiScale = double.TryParse(webUiScaleStr, out var ws) ? ws : 1.0;

        SettingsVm.LoadSettings(themeId, uiScale, webUiScale, webThemeId);

        // Initialize web server address list with persisted selection
        if (WebServerVm != null) {
            var savedAddress = await _campaignRepository.LoadSettingAsync("webSelectedAddress");
            WebServerVm.Initialize(savedAddress);
        }

        // Load window geometry
        var wxStr = await _campaignRepository.LoadSettingAsync("windowX");
        var wyStr = await _campaignRepository.LoadSettingAsync("windowY");
        var wwStr = await _campaignRepository.LoadSettingAsync("windowWidth");
        var whStr = await _campaignRepository.LoadSettingAsync("windowHeight");
        WindowState = await _campaignRepository.LoadSettingAsync("windowState");

        if (double.TryParse(wxStr, out var wx)) WindowX = wx;
        if (double.TryParse(wyStr, out var wy)) WindowY = wy;
        if (double.TryParse(wwStr, out var ww)) WindowWidth = ww;
        if (double.TryParse(whStr, out var wh)) WindowHeight = wh;

        // Load splitter layout ratios
        var diceRollerRatioStr = await _campaignRepository.LoadSettingAsync("diceRollerRatio");
        var campaignNotesRatioStr = await _campaignRepository.LoadSettingAsync("campaignNotesRatio");
        if (double.TryParse(diceRollerRatioStr, out var drr)) EncounterTrackerVm.DiceRollerColumnRatio = drr;
        if (double.TryParse(campaignNotesRatioStr, out var cnr)) EncounterTrackerVm.CampaignNotesRowRatio = cnr;

        var characters = await _campaignRepository.LoadCharactersAsync();
        EncounterTrackerVm.LoadCharacters(characters);

        var encounterState = await _campaignRepository.LoadEncounterStateAsync();
        EncounterTrackerVm.LoadEncounterState(encounterState);

        var diceHistory = await _campaignRepository.LoadDiceHistoryAsync();
        DiceRollerVm.LoadHistory(diceHistory);

        var (notes, caret) = await _campaignRepository.LoadCampaignNotesAsync();
        EncounterTrackerVm.CampaignNotesVm.MarkdownText = notes;
        EncounterTrackerVm.CampaignNotesVm.CaretPosition = caret;

        await MonsterManagerVm.InitializeAsync();
    }

    public async Task SaveCampaignAsync() {
        var characters = EncounterTrackerVm.GetCharacterModels();
        await _campaignRepository.SaveCharactersAsync(characters);

        var encounterState = EncounterTrackerVm.GetEncounterState();
        await _campaignRepository.SaveEncounterStateAsync(encounterState);

        var diceHistory = DiceRollerVm.GetHistoryResults();
        await _campaignRepository.SaveDiceHistoryAsync(diceHistory);

        await _campaignRepository.SaveCampaignNotesAsync(
            EncounterTrackerVm.CampaignNotesVm.MarkdownText,
            EncounterTrackerVm.CampaignNotesVm.CaretPosition);

        // Save window geometry
        if (WindowX.HasValue)
            await _campaignRepository.SaveSettingAsync("windowX", WindowX.Value.ToString());
        if (WindowY.HasValue)
            await _campaignRepository.SaveSettingAsync("windowY", WindowY.Value.ToString());
        if (WindowWidth.HasValue)
            await _campaignRepository.SaveSettingAsync("windowWidth", WindowWidth.Value.ToString());
        if (WindowHeight.HasValue)
            await _campaignRepository.SaveSettingAsync("windowHeight", WindowHeight.Value.ToString());
        if (WindowState != null)
            await _campaignRepository.SaveSettingAsync("windowState", WindowState);

        // Save splitter layout ratios
        if (EncounterTrackerVm.DiceRollerColumnRatio.HasValue)
            await _campaignRepository.SaveSettingAsync("diceRollerRatio",
                EncounterTrackerVm.DiceRollerColumnRatio.Value.ToString("F4"));
        if (EncounterTrackerVm.CampaignNotesRowRatio.HasValue)
            await _campaignRepository.SaveSettingAsync("campaignNotesRatio",
                EncounterTrackerVm.CampaignNotesRowRatio.Value.ToString("F4"));

        // Bestiary is now a single master DB; no paths to save
    }
}
