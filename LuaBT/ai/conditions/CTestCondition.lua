local CTestCondition = bt.Class("CTestCondition",bt.ConditionTask)
bt.CTestCondition = CTestCondition

function CTestCondition:ctor()
    bt.ConditionTask.ctor(self)
    self.name = "CTestCondition"
end

function CTestCondition:init(jsonData)
    bt.ConditionTask:init(jsonData)
end

function CTestCondition:onCheck()
    print("CTestCondition:onCheck")
    return true
end