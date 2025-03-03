using Civ2.Rules;
using Civ2engine;
using Model;

namespace Civ2.Dialogs.NewGame;

public class Difficulty : SimpleSettingsDialog
{
    public const string Title = "DIFFICULTY";
    
    public Difficulty() : base(Title, 0.085, -0.03)
    {
    }

    protected override string SetConfigValue(DialogResult result, PopupBox? popupBox)
    {
        Initialization.ConfigObject.DifficultlyLevel = result.SelectedIndex;
        return Initialization.ConfigObject.NumberOfCivs > 0 ? SelectGender.Title : NoOfCivs.Title;
    }
}