using System.Collections.Generic;
using UnityEngine;

namespace Mostervanoid
{
    /// <summary>Progresso salvo em PlayerPrefs: formas liberadas, chefes derrotados (= teleportes), forma atual.</summary>
    public static class Progress
    {
        const string KeyForms = "mv_forms";
        const string KeyBosses = "mv_bosses";
        const string KeyForm = "mv_form";

        static int formsMask = 1;
        static readonly HashSet<string> bosses = new HashSet<string>();
        public static int CurrentForm;

        public static void Load()
        {
            formsMask = PlayerPrefs.GetInt(KeyForms, 1) | 1;
            bosses.Clear();
            foreach (var s in PlayerPrefs.GetString(KeyBosses, "").Split(','))
                if (!string.IsNullOrEmpty(s)) bosses.Add(s);
            CurrentForm = PlayerPrefs.GetInt(KeyForm, 0);
            if (!IsFormUnlocked(CurrentForm)) CurrentForm = 0;
        }

        public static bool ReadOnly;   // modo de teste: não grava PlayerPrefs

        public static void Save()
        {
            if (ReadOnly) return;
            PlayerPrefs.SetInt(KeyForms, formsMask);
            PlayerPrefs.SetString(KeyBosses, string.Join(",", bosses));
            PlayerPrefs.SetInt(KeyForm, CurrentForm);
            PlayerPrefs.Save();
        }

        public static bool HasAnyProgress => formsMask != 1 || bosses.Count > 0;

        public static bool IsFormUnlocked(int form) => (formsMask & (1 << form)) != 0;
        public static void UnlockForm(int form) { formsMask |= 1 << form; Save(); }
        public static void UnlockAllForms() { formsMask = (1 << Forms.Count) - 1; Save(); }

        public static bool IsBossDefeated(string stageId) => bosses.Contains(stageId);
        public static void SetBossDefeated(string stageId) { bosses.Add(stageId); Save(); }

        public static void Reset()
        {
            formsMask = 1;
            bosses.Clear();
            CurrentForm = 0;
            Save();
        }
    }
}
