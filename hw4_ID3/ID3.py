#encode=utf-8
import time
import csv
import os
import math
from collections import defaultdict, Counter
import sqlite3 # 導入sqlite3模塊

#遞迴創建字典樹
def makedir(cur,Fdict,tmp_tree,filename,last_item,sql_str,deep,min_item,or_Fdict,fw):
    #開始做之前都要先確定一下它資料表分割是不是已經到底了，到底就要停止然後輸出決策樹那一分支的結果
    last = list(set(cur.execute("SELECT %s FROM %s WHERE%s" %(last_item,filename,sql_str[5:]))))
    if len(last) == 1 or len(last) == 0:
        fw_data = str(last) + "\n"
        fw.write(fw_data)
    else:#else的部分跟main沒什麼太大的不一樣
        entropy = defaultdict(list)
        for things in Fdict:
            total_en=0
            for i in Fdict[things]:
                tmp_en= []
                for j in result_list:
                    tmp = list(cur.execute("SELECT COUNT (%s) FROM %s WHERE `%s` IS '%s' AND `%s` is '%s'%s" %(things,filename,things,i[0],last_item,j[0],sql_str)))[0]
                    if tmp[0] == 0:
                        continue
                    tmp_en.append(tmp[0])

                total_en += countEntropy(tmp_en)
            entropy[things] = total_en/len(Fdict[things])
        
        min_item = list(entropy.keys())[list(entropy.values()).index(min(entropy.values()))]
        next_item = set(cur.execute("SELECT %s FROM  `%s` WHERE%s" %(min_item,filename,sql_str[5:])))
        del Fdict[min_item]
        
        fw_data = str(min_item) + "\n"
        fw.write(fw_data)

        or_sql_str = sql_str
        for i in next_item:
            fw_data = ""
            for j in range(0,deep):
                fw_data = fw_data+ "｜\t\t"
            if i == list(next_item)[-1]:
                fw_data =  fw_data + "└─" +str(i[0])+"──"
            else:
                fw_data = fw_data + "├─" +str(i[0])+"──"
            fw.write(fw_data)
            
            tmp_tree[min_item][i[0]] = defaultdict(dict)
            
            sql_str = or_sql_str + " AND `%s` IS '%s'" %(min_item,i[0])
            makedir(cur,Fdict,tmp_tree[min_item][i[0]],filename,last_item,sql_str,deep+1,min_item,or_Fdict,fw)
        Fdict[min_item] = or_Fdict[min_item]#這裡要把前面刪光光的Fdict一個一個補回來

#計算entropy
def countEntropy(in_list):
    if in_list.count(0) == len(in_list)-1:
        return 0
    else:
        in_list = [ value for value in in_list if value != 0]
    total = sum(in_list)
    ans = 0
    for i in in_list:
        tmp = i/total
        ans = ans - tmp*math.log2(tmp)
    return ans

if __name__ == '__main__':
    file_name = input("請輸入檔名：")
    filename = file_name[:-4]

    #紀錄時間
    start = time.time()
    print ("Start time: %.3f" %start)
    #開啟output檔
    fw = open("output_%s.txt" %filename, "a")
    # 連接資料庫文件：
    conn = sqlite3.connect("ID3.sqlite", timeout=100)
    cur = conn.cursor()
    tree = defaultdict(dict)#建一個dict存決策樹的樣子

    #開啟csv檔案，並全部存成SQL的database(使用sqlite3)
    with open(file_name) as csvfile:
        reader = csv.reader(csvfile, delimiter=',', quotechar='|')
        item = reader.__next__()
        for i in range(0,len(item)):
            #?、空白不可以存成SQL表格的欄位名稱，要換掉
            item[i] = item[i].replace("?","")
            item[i] = item[i].replace(" ","_")
            if i == len(item)-1:
                last_item = item[i]#把最後一個欄位視為決策樹的結果
        
        # 建立表格：
        cur.execute("CREATE TABLE %s( ID INT(5) NOT NULL, %s VARCHAR(50))" %(filename,item[0]))
        
        for i in range(1,len(item)):#新增每個欄位的結構
            tmp = item[i]
            cur.execute("ALTER TABLE  `%s` ADD  `%s` VARCHAR(50)" %(filename,tmp))
        line = 0
        for row in reader:#新增每筆資料
            line = line+1
            insert_str = str(line)
            for i in row:
                insert_str = insert_str + ",'%s'" %(i)
            cur.execute("INSERT INTO `%s` VALUES (%s)" %(filename,insert_str))

        # 保存更改信息：
        conn.commit()
        
    Fdict = defaultdict(list)#建一個dict去存每個欄位有哪幾種項目(除了最後一欄的結果)
    for col in item:
        if col == last_item:
            continue
        Fdict[col] = set(cur.execute("SELECT %s FROM  `%s`" %(col,filename)))

    #建一個list去存最後一欄的結果有哪幾種(EX yes、no)
    result_list = set(cur.execute("SELECT %s FROM  `%s`" %(last_item,filename)))
    
    #計算各個項目的entropy
    entropy = defaultdict(list)#建一個dict去存每個欄位的平均entropy
    for things in Fdict:
        total_en=0
        for i in Fdict[things]:
            tmp_en= []
            for j in result_list:
                tmp = list(cur.execute("SELECT COUNT (%s) FROM %s WHERE `%s` IS '%s' AND `%s` is '%s'" %(things,filename,things,i[0],last_item,j[0])))[0]
                tmp_en.append(tmp[0])
            total_en += countEntropy(tmp_en)
        entropy[things] = total_en/len(Fdict[things])

    #找出其中entropy最小的
    min_item = list(entropy.keys())[list(entropy.values()).index(min(entropy.values()))]

    #看最小的那一欄要畫出幾種分支
    next_item = set(cur.execute("SELECT %s FROM  `%s`" %(min_item,filename)))
    #這個分支要畫了，下一次就不用再畫它所以要刪掉
    del Fdict[min_item]
    or_Fdict = Fdict.copy()#原本的dict還是要複製一個，因為del Fdict就算用遞迴他也會幫你push進去，你得自己堆疊回去
    #entropy.clear()

    #這裡畫決策樹到txt檔
    fw_data = min_item + "\n"
    fw.write(fw_data)
    print(min_item)
    deep = 1#記錄決策樹的深度，才知道寫檔要tab幾次

    #開始遞迴畫決策樹了
    for ne in next_item:
        tree[min_item][ne[0]] = defaultdict(dict)#這是一直往tree的深處走，以便存值建成一個多維的dict
        fw_data = ""
        if ne == list(next_item)[-1]:
            fw_data =  fw_data + "└─" +str(ne[0])+"──"
        else:
            fw_data = fw_data + "├─" +str(ne[0])+"──"
        fw.write(fw_data)
        print("|----",ne[0])
        sql_str = " AND `%s` IS '%s'" %(min_item,ne[0])#決策樹每次往深處走，sql的篩選條件都會多一種欄位
        tmp_tree = tree[min_item][ne[0]]
        makedir(cur,Fdict,tmp_tree,filename,last_item,sql_str,deep,min_item,or_Fdict,fw)

#關閉檔案&資料庫    
cur.close()
conn.close()
fw.close()
end = time.time()
usetime = end - start
print ("End time: %.3f" %end)
print("花費",usetime,"秒")
print("DONE!")
"""
