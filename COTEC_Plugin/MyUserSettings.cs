using System;
using System.Configuration;
using System.Drawing;

public class MyUserSettings : ApplicationSettingsBase
{
    [UserScopedSettingAttribute()]
    [DefaultSettingValue("point")]
    public String point_selection_type
    {
        get { return (String)this["point_selection_type"]; }
        set { this["point_selection_type"] = value; }
    }

    [UserScopedSettingAttribute()]
    [DefaultSettingValue("0.00")]
    public String default_pourcentage_CCD
    {
        get { return (String)this["default_pourcentage_CCD"]; }
        set { this["default_pourcentage_CCD"] = value; }
    }
}