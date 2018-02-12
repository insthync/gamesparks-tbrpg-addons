﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using GameSparks.Core;
using GameSparks.Api.Requests;
#if UNITY_EDITOR
using UnityEditor;
#endif


public partial class GSGameService : BaseGameService
{
    private void Awake()
    {
        StartCoroutine(WaitForGS());
    }
    
    IEnumerator WaitForGS()
    {
        while (!GS.Available)
            yield return 0;

        Debug.Log("Gamesparks ready.");
    }

#if UNITY_EDITOR
    [ContextMenu("Export Game Database")]
    public void ExportGameDatabase()
    {
        var gameInstance = FindObjectOfType<GameInstance>();
        if (gameInstance == null)
        {
            Debug.LogError("Cannot export game database, no game instance found");
            return;
        }
        var gameDatabase = gameInstance.gameDatabase;
        if (gameDatabase == null)
        {
            Debug.LogError("Cannot export game database, no game database found");
            return;
        }
        gameDatabase.Setup();
        var itemsJson = "";
        var currenciesJson = "";
        var staminasJson = "";
        var formationsJson = "";
        var stagesJson = "";
        var lootBoxesJson = "";
        var startItemsJson = "";
        var startCharactersJson = "";
        var unlockStagesJson = "";

        foreach (var item in gameDatabase.Items)
        {
            if (!string.IsNullOrEmpty(itemsJson))
                itemsJson += ",";
            itemsJson += "\"" + item.Key + "\":" + item.Value.ToJson();
        }
        itemsJson = "{" + itemsJson + "}";

        currenciesJson = "{\"SOFT_CURRENCY\":\"" + gameDatabase.softCurrency.id + "\", \"HARD_CURRENCY\":\"" + gameDatabase.hardCurrency.id + "\"}";
        staminasJson = "{\"STAGE\":" + gameDatabase.stageStamina.ToJson() + "}";

        foreach (var entry in gameDatabase.Formations)
        {
            if (!string.IsNullOrEmpty(formationsJson))
                formationsJson += ",";
            formationsJson += "\"" + entry.Key + "\"";
        }
        formationsJson = "[" + formationsJson + "]";

        foreach (var entry in gameDatabase.Stages)
        {
            if (!string.IsNullOrEmpty(stagesJson))
                stagesJson += ",";
            stagesJson += "\"" + entry.Key + "\":" + entry.Value.ToJson();
        }
        stagesJson = "{" + stagesJson + "}";

        foreach (var entry in gameDatabase.LootBoxes)
        {
            if (!string.IsNullOrEmpty(lootBoxesJson))
                lootBoxesJson += ",";
            lootBoxesJson += "\"" + entry.Key + "\":" + entry.Value.ToJson();
        }
        lootBoxesJson = "{" + lootBoxesJson + "}";

        foreach (var entry in gameDatabase.startItems)
        {
            if (entry == null || entry.item == null)
                continue;
            if (!string.IsNullOrEmpty(startItemsJson))
                startItemsJson += ",";
            startItemsJson += entry.ToJson();
        }
        startItemsJson = "[" + startItemsJson + "]";

        foreach (var entry in gameDatabase.startCharacters)
        {
            if (entry == null)
                continue;
            if (!string.IsNullOrEmpty(startCharactersJson))
                startCharactersJson += ",";
            startCharactersJson += "\"" + entry.Id + "\"";
        }
        startCharactersJson = "[" + startCharactersJson + "]";

        foreach (var entry in gameDatabase.unlockStages)
        {
            if (entry == null)
                continue;
            if (!string.IsNullOrEmpty(unlockStagesJson))
                unlockStagesJson += ",";
            unlockStagesJson += "\"" + entry.Id + "\"";
        }
        unlockStagesJson = "[" + unlockStagesJson + "]";

        var jsonCombined = "{\"items\":" + itemsJson + "," +
            "\"currencies\":" + currenciesJson + "," +
            "\"staminas\":" + staminasJson + "," +
            "\"formations\":" + formationsJson + "," +
            "\"stages\":" + stagesJson + "," +
            "\"lootBoxes\":" + lootBoxesJson + "," +
            "\"startItems\":" + startItemsJson + "," +
            "\"startCharacters\":" + startCharactersJson + "," +
            "\"unlockStages\":" + unlockStagesJson + "," +
            "\"playerMaxLevel\":" + gameDatabase.playerMaxLevel + "," +
            "\"playerExpTable\":" + gameDatabase.playerExpTable.ToJson() + "," +
            "\"revivePrice\":" + gameDatabase.revivePrice + "," +
            "\"resetItemLevelAfterEvolve\":" + (gameDatabase.resetItemLevelAfterEvolve ? 1 : 0) + "}";

        var cloudCodes = "var gameDatabase = " + jsonCombined + ";";
        var path = EditorUtility.SaveFilePanel("Export Game Database", Application.dataPath, "GAME_DATA", "js");
        if (path.Length > 0)
            File.WriteAllText(path, cloudCodes);
    }
#endif

    protected LogEventRequest GetGSEventRequest(string target, GSRequestData data = null)
    {
        if (data == null)
            data = new GSRequestData();
        var request = new LogEventRequest();
        request.SetEventKey("SERVICE_EVENT");
        request.SetEventAttribute("TARGET", target);
        request.SetEventAttribute("DATA", data);
        return request;
    }

    protected override void DoGetAuthList(string playerId, string loginToken, UnityAction<AuthListResult> onFinish)
    {
        var result = new AuthListResult();
        onFinish(result);
    }

    protected override void DoGetItemList(string playerId, string loginToken, UnityAction<ItemListResult> onFinish)
    {
        var result = new ItemListResult();
        var request = GetGSEventRequest("GetItemList");
        request.Send((response) =>
        {
            GSData scriptData = response.ScriptData;
            if (scriptData != null && scriptData.ContainsKey("list"))
            {
                var list = scriptData.GetGSDataList("list");
                foreach (var entry in list)
                {
                    var resultEntry = new PlayerItem();
                    PlayerItem.CloneTo(JsonUtility.FromJson<DbPlayerItem>(entry.JSON), resultEntry);
                    result.list.Add(resultEntry);
                }
                onFinish(result);
            }
        });
    }

    protected override void DoGetCurrencyList(string playerId, string loginToken, UnityAction<CurrencyListResult> onFinish)
    {
        var result = new CurrencyListResult();
        var request = GetGSEventRequest("GetCurrencyList");
        request.Send((response) =>
        {
            GSData scriptData = response.ScriptData;
            if (scriptData != null && scriptData.ContainsKey("list"))
            {
                var list = scriptData.GetGSDataList("list");
                foreach (var entry in list)
                {
                    var resultEntry = new PlayerCurrency();
                    PlayerCurrency.CloneTo(JsonUtility.FromJson<DbPlayerCurrency>(entry.JSON), resultEntry);
                    result.list.Add(resultEntry);
                }
                onFinish(result);
            }
        });
    }

    protected override void DoGetStaminaList(string playerId, string loginToken, UnityAction<StaminaListResult> onFinish)
    {
        var result = new StaminaListResult();
        var request = GetGSEventRequest("GetStaminaList");
        request.Send((response) =>
        {
            GSData scriptData = response.ScriptData;
            if (scriptData != null && scriptData.ContainsKey("list"))
            {
                var list = scriptData.GetGSDataList("list");
                foreach (var entry in list)
                {
                    var resultEntry = new PlayerStamina();
                    PlayerStamina.CloneTo(JsonUtility.FromJson<DbPlayerStamina>(entry.JSON), resultEntry);
                    result.list.Add(resultEntry);
                }
                onFinish(result);
            }
        });
    }

    protected override void DoGetFormationList(string playerId, string loginToken, UnityAction<FormationListResult> onFinish)
    {
        var result = new FormationListResult();
        var request = GetGSEventRequest("GetFormationList");
        request.Send((response) =>
        {
            GSData scriptData = response.ScriptData;
            if (scriptData != null && scriptData.ContainsKey("list"))
            {
                var list = scriptData.GetGSDataList("list");
                foreach (var entry in list)
                {
                    var resultEntry = new PlayerFormation();
                    PlayerFormation.CloneTo(JsonUtility.FromJson<DbPlayerFormation>(entry.JSON), resultEntry);
                    result.list.Add(resultEntry);
                }
                onFinish(result);
            }
        });
    }

    protected override void DoGetUnlockItemList(string playerId, string loginToken, UnityAction<UnlockItemListResult> onFinish)
    {
        var result = new UnlockItemListResult();
        var request = GetGSEventRequest("GetUnlockItemList");
        request.Send((response) =>
        {
            GSData scriptData = response.ScriptData;
            if (scriptData != null && scriptData.ContainsKey("list"))
            {
                var list = scriptData.GetGSDataList("list");
                foreach (var entry in list)
                {
                    var resultEntry = new PlayerUnlockItem();
                    PlayerUnlockItem.CloneTo(JsonUtility.FromJson<DbPlayerUnlockItem>(entry.JSON), resultEntry);
                    result.list.Add(resultEntry);
                }
                onFinish(result);
            }
        });
    }

    protected override void DoGetClearStageList(string playerId, string loginToken, UnityAction<ClearStageListResult> onFinish)
    {
        var result = new ClearStageListResult();
        var request = GetGSEventRequest("GetClearStageList");
        request.Send((response) =>
        {
            GSData scriptData = response.ScriptData;
            if (scriptData != null && scriptData.ContainsKey("list"))
            {
                var list = scriptData.GetGSDataList("list");
                foreach (var entry in list)
                {
                    var resultEntry = new PlayerClearStage();
                    PlayerClearStage.CloneTo(JsonUtility.FromJson<DbPlayerClearStage>(entry.JSON), resultEntry);
                    result.list.Add(resultEntry);
                }
                onFinish(result);
            }
        });
    }
}
