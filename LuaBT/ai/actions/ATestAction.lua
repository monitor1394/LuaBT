local ATestAction = bt.Class("ATestAction",bt.ActionTask)
bt.ATestAction = ATestAction

function ATestAction:ctor()
    bt.ActionTask.ctor(self)
    self.name = "ATestAction"
end

function ATestAction:init(jsonData)
end

function ATestAction:onExecute()
    print("ATestAction:onExecute")
    self:endAction(true)
end