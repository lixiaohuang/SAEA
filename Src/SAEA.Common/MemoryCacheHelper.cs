﻿/****************************************************************************
*Copyright (c) 2018 yswenli All Rights Reserved.
*CLR版本： 4.0.30319.42000
*机器名称：WENLI-PC
*公司名称：yswenli
*命名空间：SAEA.Commom
*文件名： MemoryCache
*版本号： v5.0.0.1
*唯一标识：13ed79e2-020d-45aa-a67f-ec00cf30da2d
*当前的用户域：WENLI-PC
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2018/3/13 9:31:54
*描述：
*
*=====================================================================
*修改标记
*修改时间：2018/3/13 9:31:54
*修改人： yswenli
*版本号： v5.0.0.1
*描述：
*
*****************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SAEA.Common
{
    /// <summary>
    /// 自定义过期缓存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MemoryCacheHelper<T> : IDisposable
    {
        ConcurrentDictionary<string, MemoryCacheItem<T>> _dic;

        bool _disposed = false;

        object _synclocker = new object();


        public MemoryCacheHelper()
        {
            _dic = new ConcurrentDictionary<string, MemoryCacheItem<T>>();
        }

        public void Set(string key, T value, TimeSpan timeOut)
        {
            var mc = new MemoryCacheItem<T>() { Key = key, Value = value, Expired = DateTimeHelper.Now.AddSeconds(timeOut.TotalSeconds) };
            _dic.AddOrUpdate(key, mc, (k, v) => { return mc; });
        }

        public T Get(string key)
        {
            _dic.TryGetValue(key, out MemoryCacheItem<T> mc);
            if (mc != null && mc.Value != null)
            {
                if (mc.Expired <= DateTimeHelper.Now)
                {
                    Del(key);
                }
                else
                {
                    return mc.Value;
                }
            }
            return default(T);
        }

        public void Active(string ID, TimeSpan timeOut)
        {
            lock (_synclocker)
            {
                var item = Get(ID);
                if (item != null)
                {
                    Set(ID, item, timeOut);
                }
            }
        }


        public void Del(string key)
        {
            _dic.TryRemove(key, out MemoryCacheItem<T> mc);
        }

        public IEnumerable<T> List
        {
            get
            {
                return _dic.Values.Select(b => b.Value);
            }
        }

        public void Clear()
        {
            _dic.Clear();
        }

        public void Dispose()
        {
            _disposed = true;
            _dic.Clear();
            _dic = null;
        }
    }
}
