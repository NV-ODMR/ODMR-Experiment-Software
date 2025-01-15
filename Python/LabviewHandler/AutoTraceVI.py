# 调用进行Autotrace的Labview vi

from labview_automation import LabVIEW

lv = LabVIEW()
lv.start()
lv.kill()

AutoTracePath = 'E:\\ODMR_labview\\磁场调平 2016版本\\AutoTrace_ESEEM.vi'


def AutoTrace():
    try:
        lv.start()
        with lv.client() as l:
            indicator = l.run_vi_synchronous(AutoTracePath, {}, open_frontpanel=True)
            lv.kill()
    except:
        return
