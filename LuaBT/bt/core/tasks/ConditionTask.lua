
local ConditionTask = bt.Class("ConditionTask",bt.Task)
bt.ConditionTask = ConditionTask

function ConditionTask:ctor()
    bt.Task.ctor(self)
    self.name = "ConditionTask"
    self.invert = false
end

function ConditionTask:init(jsonData)
    if jsonData._invert then
        self.invert = jsonData._invert
    end
end

function ConditionTask:enable(agent,blackboard)
    if self:set(agent,blackboard) then
        self:onEnable()
    end
end

function ConditionTask:disable()
    self.isActive = false
    self:onDisable()
end

function ConditionTask:checkCondition(agent,blackboard)
    if not self.isActive then
        return false
    end
    if not self:set(agent,blackboard) then
        return false
    end
    if self.invert then
        return not self:onCheck()
    else
        return self:onCheck()
    end
end

function ConditionTask:onCheck()
    return true
end

function ConditionTask:info()
    if self.invert then
        return "If !" .. self.name
    else
        return "If " .. self.name
    end
end