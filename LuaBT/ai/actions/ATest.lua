local ATest = bt.Class("ATest",bt.ActionTask)
bt.ATest = ATest

function ATest:ctor()
    bt.ActionTask.ctor(self)
    self.name = "ATest"
end

function ATest:init(jsonData)
end

function ATest:onExecute()
    local success = APIAction.test(self.agent)
    self:endAction(true)
end