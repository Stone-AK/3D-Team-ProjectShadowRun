using System;
using UnityEngine;

public enum DNSkyboxType
{
    Morning,
    Day,
    Rainey,
    Dusk,
    Night
}

[Serializable]
public struct DNMoodSetting
{
    public Material Material_Skybox;
    public Color Color_Skybox;
    public Color Color_EquatorColor;
    public Color Color_GroundColor;
    public GameObject GameObject_LightBoxGroup;
}

public class DNSkyBoxSwitcher : MonoBehaviour
{
    [SerializeField] private DNMoodSetting SettingData_Day;
    [SerializeField] private DNMoodSetting SettingData_Morning;
    [SerializeField] private DNMoodSetting SettingData_Rainey;
    [SerializeField] private DNMoodSetting SettingData_Dusk;
    [SerializeField] private DNMoodSetting SettingData_Night;


    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeSkybox(DNSkyboxType.Morning);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeSkybox(DNSkyboxType.Day);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChangeSkybox(DNSkyboxType.Rainey);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ChangeSkybox(DNSkyboxType.Dusk);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ChangeSkybox(DNSkyboxType.Night);
        }
    }

    public void ChangeSkybox(DNSkyboxType boxType)
    {
        SettingData_Morning.GameObject_LightBoxGroup.SetActive(false);
        SettingData_Day.GameObject_LightBoxGroup.SetActive(false);
        SettingData_Rainey.GameObject_LightBoxGroup.SetActive(false);
        SettingData_Dusk.GameObject_LightBoxGroup.SetActive(false);
        SettingData_Night.GameObject_LightBoxGroup.SetActive(false);

        switch (boxType)
        {
            case DNSkyboxType.Morning:
            SetMood(SettingData_Morning);
            SettingData_Morning.GameObject_LightBoxGroup.SetActive(true);
            break;
            case DNSkyboxType.Day:
            SetMood(SettingData_Day);
            SettingData_Day.GameObject_LightBoxGroup.SetActive(true);
            break;
            case DNSkyboxType.Rainey:
            SetMood(SettingData_Rainey);
            SettingData_Rainey.GameObject_LightBoxGroup.SetActive(true);
            break;
            case DNSkyboxType.Dusk:
            SetMood(SettingData_Dusk);
            SettingData_Dusk.GameObject_LightBoxGroup.SetActive(true);
            break;
            case DNSkyboxType.Night:
            SetMood(SettingData_Night);
            SettingData_Night.GameObject_LightBoxGroup.SetActive(true);
            break;
        }
    }

    private void SetMood(DNMoodSetting data)
    {
        RenderSettings.skybox = data.Material_Skybox;
        RenderSettings.ambientSkyColor = data.Color_Skybox;
        RenderSettings.ambientEquatorColor = data.Color_EquatorColor;
        RenderSettings.ambientGroundColor = data.Color_GroundColor;
    }
}

