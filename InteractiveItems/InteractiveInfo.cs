using UnityEngine;

namespace Dead_Earth.Scripts.InteractiveItems
{
  /// <summary>
  /// interactive item that simply displays information on the HUD
  /// </summary>
  public class InteractiveInfo : InteractiveItem
  {
    [TextArea(3, 10)] [SerializeField] private string infoText;

    /// <summary>
    /// returns the text of the information of the interactive item
    /// </summary>
    /// <returns></returns>
    public override string GetText()
    {
      return infoText;
    }
  }
}