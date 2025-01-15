import os

from labview_automation import LabVIEW

GlobalPath = 'E:\\ODMR_labview\\磁场调平 2016版本\\Global  Parameter_ESEEM.vi'

def Init():
	lv = LabVIEW()
	# 打开全局变量文件，避免出现参数修改失败的错误
	os.startfile(GlobalPath)