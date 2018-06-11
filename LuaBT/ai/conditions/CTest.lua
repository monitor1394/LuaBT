local CTest = bt.Class("CTest",bt.ConditionTask)
bt.CTest = CTest

function CTest:ctor()
    bt.ConditionTask.ctor(self)
    self.name = "CTest"
end

function CTest:init(jsonData)
    bt.ConditionTask:init(jsonData)
end

function CTest:onCheck()
    local flag = APICondition.isTest(self.agent)
    return true
end