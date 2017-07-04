import time
import csv
from collections import defaultdict
from itertools import combinations

table = defaultdict(int)
CI = defaultdict(int)
file_name = input("請輸入檔名：")
sup = int(input("請輸入門檻值："))
index_list = []
times = 1

start = time.time()
print ("Start time: %.3f" %start)

fw = open("tmp.txt","w")
with open(file_name,"r") as f:
    for line in csv.reader(f):
        dw = ""
        for item in line:
            dw = dw + item.strip() + ","
            table[item] = table[item] + 1
        dw = dw[:-1] + "\n"
        fw.write(dw)
fw.close()
table = { key : value for key,value in table.items() if value >= sup}

total = len(table)
print(total)

fw = open("output_%s" %file_name, "a")
for key in table.keys():
    fw_data = key +":"+ str(table[key])+"\n"
    fw.write(fw_data)
fw.close()
 
index_list = list(table.keys())

times = times + 1

while index_list != []:
    del table
    table = defaultdict(int)
    with open("tmp.txt","r") as f:
        for line in csv.reader(f):
            print(line)
            com_lst = set(index_list) & set(line)
            tmp_list = list(combinations(com_lst, times))
            
            for item in tmp_list:
                table[tuple(sorted(item))] += 1

    if bool(table.values()) == False:
        break
    
    table = { key : value for key,value in table.items() if value >= sup}
    total = len(table) + total
    print(total)

    index_list.clear()
    del CI
    CI = defaultdict(int)
    fw = open("output_%s" %file_name, "a")
    for key in table.keys():
        fw_data = str(key) +":"+ str(table[key])+"\n"
        for item in key:
            CI[item] +=1
        fw.write(fw_data)
    fw.close()

    print(times)

    CI = { key : value for key,value in CI.items() if value >= times}
    print(CI.keys())
    index_list = list(CI.keys())
    
    times = times + 1
    
end = time.time()
elapsed = end - start
print ("End time:%.3f" %end)
print ("Time taken: %.3f seconds." %(elapsed))
print ("Total",total)
