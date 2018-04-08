
-----------------------------------------------------------------------------------------
-- Decorators
-- WaitUntil

-- Returns Running until the assigned condition becomes true
-----------------------------------------------------------------------------------------

local WaitUntil = bt.Class("WaitUntil",bt.BTDecorator)
bt.WaitUntil = WaitUntil
    
function WaitUntil:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "WaitUntil"
    self.condition = nil
    self.accessed = false
end

function WaitUntil:init(jsonData)
    if jsonData._condition then
        local Cls = bt.getCls(jsonData._condition["$type"], jsonData._condition)
        self.condition = Cls.new()
        self.condition:init(jsonData._condition)
    end
end

function WaitUntil:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Resting
    end

    if self.condition == nil then
        return decoratedConnection:execute(agent,blackboard)
    end

    if self.accessed then
        return decoratedConnection:execute(agent,blackboard)
    end

    if self.condition:checkCondition(agent,blackboard) then
        self.accessed = true
    end

    if self.accessed then
        return decoratedConnection:execute(agent,blackboard)
    else 
        return bt.Status.Running
    end
end

function WaitUntil:onReset()
    self.accessed = false
end

function WaitUntil:destroy()
    if self.condition ~= nil then
        self.condition:destroy()
        self.condition = nil 
    end
end
