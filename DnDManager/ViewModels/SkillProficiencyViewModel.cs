using CommunityToolkit.Mvvm.ComponentModel;

namespace DnDManager.ViewModels;

/// <summary>One editable skill-proficiency row (skill name + total bonus) in the bestiary editor.</summary>
public partial class SkillProficiencyViewModel : ObservableObject {
    public string Skill { get; }

    [ObservableProperty]
    private string _bonusText;

    public SkillProficiencyViewModel(string skill, string bonusText) {
        Skill = skill;
        _bonusText = bonusText;
    }
}
