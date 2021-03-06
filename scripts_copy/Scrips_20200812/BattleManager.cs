﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using UnityEngine.SceneManagement;
namespace PCRCaculator.Battle
{
    public enum eGameBattleState { PREPARING = 0,FIGHTING=1,WIN=2,LOSE=3,TIME_UP=4 }
    public enum eCheatValueType { FORCE_CRITICAL = 1,FORCE_IGNOR_DODGE = 2,FORCE_IGNOR_DEF = 3,FORCE_ALIVE = 4}
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance;
        private static BattleUIManager uIManager;
        public Transform spineParent;
        private List<UnitCtrl> playersList = new List<UnitCtrl>();
        private List<UnitCtrl> enemiesList = new List<UnitCtrl>();
        private List<UnitCtrl> blackOutUnitList = new List<UnitCtrl>();

        private eGameBattleState gameState;
        private List<UnitData> playerData;
        private List<UnitData> enemyData;
        private float battleTime = 0;
        private float frameRate = 60;
        private bool isPause;
        private int energyStackValueDefeat = 200;//击杀一个单位获取的TP
        private bool isPauseForUB = false;
        private List<UnitCtrl> currentUBingUnit = new List<UnitCtrl>();//当前正在放UB的单位列表
        private Dictionary<int, List<PLFCLLHLDOO>> fieldDataDictionary = new Dictionary<int, List<PLFCLLHLDOO>>();
        public bool isPlayerForceCritical = false;
        public bool isPlayerForceIgnoreDodge = false;
        public bool isPlayerForceIgnorDef = false;
        public bool isPlayerForceAlive = true;

        public bool isEnemyForceCritical = false;
        public bool isEnemyForceIgnoreDodge = false;
        public bool isEnemyForceIgnorDef = false;
        public bool isEnemyForceAlive = false;
        public bool ShowErrorMessage = false;

        public bool skipCutInMovie = true;

        public eGameBattleState GameState { get => gameState;}
        public List<UnitCtrl> PlayersList { get => playersList;}
        public List<UnitCtrl> EnemiesList { get => enemiesList;}
        public List<UnitCtrl> BlackOutUnitList { get => blackOutUnitList; set => blackOutUnitList = value; }
        public float BattleTime { get => battleTime; set => battleTime = value; }
        public float FrameRate { get => frameRate; set => frameRate = value; }
        public bool IsPause { get => isPause; set => isPause = value; }
        public float DeltaTimeForPause { get => IsPause ? 0 :Time.deltaTime; }
        public float DeltaTimeForPause_BUFF { get => IsPause||isPauseForUB ? 0 : Time.deltaTime; }
        public float DeltaTimeForUB { get => Time.deltaTime; }
        public int EnergyStackValueDefeat { get => energyStackValueDefeat; set => energyStackValueDefeat = value; }

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            gameState = eGameBattleState.PREPARING;
            uIManager = BattleUIManager.Instance;
            if(MainManager.Instance == null)
            {                
                return;
            }
            MainManager.Instance.GetBattleData(out playerData,out enemyData);
            CreateSpines(playerData, enemyData);
            uIManager.SetUI();
            gameState = eGameBattleState.FIGHTING;
        }
        private void Update()
        {
            if (isPause) { return; }
            BattleTime += Time.deltaTime;
        }
        public void Pause()
        {
            if (isPauseForUB)
            {
                return;
            }
            foreach (UnitCtrl a in PlayersList)
            {
                a.Pause();
            }
            foreach(UnitCtrl b in EnemiesList)
            {
                b.Pause();
            }
            IsPause = !IsPause;
        }
        public void Pause(bool pause)
        {
            if(IsPause == pause)
            {
                return;
            }
            Pause();
        }
        public void PauseForUB(UnitCtrl source,bool start)
        {
            if (start)
            {                
                if (!isPauseForUB)
                {
                    isPauseForUB = true;
                    currentUBingUnit.Add(source);
                }
            }
            else
            {
                currentUBingUnit.Remove(source);
                if (currentUBingUnit.Count == 0)
                {
                    isPauseForUB = false;
                }
                else
                {
                    PauseForUB_0(source, start);
                    foreach (UnitCtrl unit in currentUBingUnit)
                    {
                        PauseForUB_0(unit, true);
                    }
                    return;
                }
            }
            PauseForUB_0(source, start);
        }
        private void PauseForUB_0(UnitCtrl source,bool start)
        {
            foreach (UnitCtrl a in PlayersList)
            {
                a.Pause(source, start);
            }
            foreach (UnitCtrl b in EnemiesList)
            {
                b.Pause(source, start);
            }

        }
        public int Random(int min,int max)
        {
            if (min >= max) { return min; }
            return UnityEngine.Random.Range(min, max);
        }
        /// <summary>
        /// 0-100随机整数
        /// </summary>
        /// <returns>0-100随机整数</returns>
        public int Random()
        {
            return UnityEngine.Random.Range(0, 100);
        }
        public void ExitButton()
        {
            SceneManager.LoadScene("BeginScene");
        }
        public void UnitDie(UnitCtrl unitCtrl)
        {
            if (unitCtrl.IsOther)
            {
                EnemiesList.Remove(unitCtrl);
            }
            else
            {
                PlayersList.Remove(unitCtrl);
            }
            if (PlayersList.Count <= 0)
            {
                gameState = eGameBattleState.LOSE;
                foreach(UnitCtrl a in EnemiesList)
                {
                    a.SetVictory();
                }
                foreach (UnitCtrl b in PlayersList)
                {
                    b.SetVictory(false);
                }
                Debug.LogError("失败!");
            }
            else if (EnemiesList.Count <= 0)
            {
                gameState = eGameBattleState.WIN;
                foreach (UnitCtrl a in EnemiesList)
                {
                    a.SetVictory(false);
                }
                foreach (UnitCtrl b in PlayersList)
                {
                    b.SetVictory();
                }
                Debug.LogError("胜利!");
            }
        }
        public bool GetCheatValues(eCheatValueType cheatValueType,bool isOther)
        {
            switch (cheatValueType)
            {
                case eCheatValueType.FORCE_CRITICAL:
                    return isOther ? isEnemyForceCritical : isPlayerForceCritical;
                case eCheatValueType.FORCE_IGNOR_DODGE:
                    return isOther ? isEnemyForceIgnoreDodge : isPlayerForceIgnoreDodge;
                case eCheatValueType.FORCE_IGNOR_DEF:
                    return isOther ? isEnemyForceIgnorDef : isPlayerForceIgnorDef;
                case eCheatValueType.FORCE_ALIVE:
                    return isOther ? isEnemyForceAlive : isPlayerForceAlive;
            }
            return false;
        }
        /// <summary>
        /// 创建敌我双方的战斗小人
        /// </summary>
        /// <param name="players">我方，已经排好顺序</param>
        /// <param name="enemies">敌方，已经排好顺序</param>
        private void CreateSpines(List<UnitData> players, List<UnitData> enemies)
        {
            int i = 0;
            int j = 0;
            foreach(UnitData a in players)
            {
                CreateSpine(a,i,false);
                i++;
            }
            foreach(UnitData b in enemies)
            {
                CreateSpine(b, j, true);
                j++;
            }
        }
        private void CreateSpine(UnitData unitData,int posid,bool isother)
        {
            int unitid = unitData.unitId;
            SkeletonDataAsset dataAsset = ScriptableObject.CreateInstance<SkeletonDataAsset>();
            dataAsset = Resources.Load<SkeletonDataAsset>("Unit/" + unitid + "/" + unitid + "_SkeletonData");
            var sa = SkeletonAnimation.NewSkeletonAnimationGameObject(dataAsset); // Spawn a new SkeletonAnimation GameObject.
            sa.gameObject.name = unitid.ToString();
            sa.Initialize(false);
            sa.transform.SetParent(spineParent);
            UnitCtrl ctrl = sa.gameObject.AddComponent<UnitCtrl>();
            if (!isother)
            {
                playersList.Add(ctrl);
            }
            else
            {
                enemiesList.Add(ctrl);
            }
            BattleUnitBaseSpineController unitActionController = sa.gameObject.AddComponent<BattleUnitBaseSpineController>();

            unitActionController.SetOnAwake(dataAsset, unitid,ctrl);
            ctrl.SetOnAwake(unitActionController,unitData,isother);
            ctrl.SetPosition(new Vector3Int(560 + 200 * posid, posid,0));
            ctrl.SetState(eActionState.GAMESTART,0);
            ctrl.OnDead = UnitDie;
        }
        public void ExecField(PLFCLLHLDOO PFDAEFDOBIP, int OJHBHHCOAGK)
        {
            if (fieldDataDictionary.ContainsKey(OJHBHHCOAGK))
            {
                fieldDataDictionary[OJHBHHCOAGK].Add(PFDAEFDOBIP);
            }
            else
            {
                List<PLFCLLHLDOO> list = new List<PLFCLLHLDOO>();
                list.Add(PFDAEFDOBIP);
                fieldDataDictionary.Add(OJHBHHCOAGK, list);
            }
            List<UnitCtrl> targetList = PFDAEFDOBIP.FieldTargetType == eFieldTargetType.ENEMY ? EnemiesList : PlayersList;
            PFDAEFDOBIP.AllTargetList = targetList;
            PFDAEFDOBIP.StartField();
        }
    }
}