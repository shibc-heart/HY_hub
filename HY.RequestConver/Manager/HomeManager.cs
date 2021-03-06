﻿using HY.Client.Entity;
using HY.RequestConver.InterFace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HY.RequestConver.Manager
{
    public class HomeManager : IHome
    {
        public Task<ServiceResponse> GetCommonUseGames()
        {
            try
            {
                var genrator = Task.Run(() => Network.ApiGet("home", "getCommonUseGames"));
                return genrator;
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// 获取推荐游戏
        /// </summary>
        /// <returns></returns>
        public Task<ServiceResponse> GetHomeGames()
        {
            try
            {
                var genrator = Task.Run(() => Network.ApiGet("home", "getHomeGames"));
                return genrator;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public Task<ServiceResponse> UpdateCommomUseGames(List<int> gameIdList, string type)
        {
            try
            {
                Dictionary<string, object> dic = new Dictionary<string, object> { { "type", type} };
                string gameIds = string.Empty;
                foreach (var item in gameIdList)
                {
                    gameIds += item+ "|";
                }
                dic.Add("gameIds", gameIds.TrimEnd(Convert.ToChar("|")));
                var genrator = Task.Run(() => Network.ApiPost("home", "updateCommomUseGames", dic));
                return genrator;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
