using CommunityToolkit.Mvvm.ComponentModel;

namespace DnDManager.ViewModels;

/// <summary>One editable saving-throw override row in the bestiary editor (blank = derive from ability mod).</summary>
public partial class SaveProficiencyViewModel : ObservableObject {
    public string Code { get; }

    [ObservableProperty]
    private string _bonusText;

    public SaveProficiencyViewModel(string code, string bonusText) {
        Code = code;
        _bonusText = bonusText;
    }
}
