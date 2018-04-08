
-----------------------------------------------------------------------------------------
-- Decorators
-- Interruptor

-- Interrupt the child node and return Failure if the condition is or becomes true while 
-- running. Otherwise execute and return the child Status
-----------------------------------------------------------------------------------------

local Interruptor = bt.Class("Interruptor",bt.BTDecorator)
bt.Interruptor = Interruptor
    
function Interruptor:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "Interruptor"
    self.condition = nil
end

function Interruptor:init(jsonData)
    if jsonData._condition then
        local Cls = bt.getCls(jsonData._condition["$type"], jsonData._condition)
        self.condition = Cls.new()
        self.condition:init(jsonData._condition)
    end
end

function Interruptor:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Resting
    end
    if self.condition == nil or self.condition:checkCondition(agent,blackboard) == false then
        return decoratedConnection:execute(agent,blackboard)
    end
    if decoratedConnection.status == bt.Status.Running then
        decoratedConnection:reset(true)
    end
    return bt.Status.Failure
end

function Interruptor:destroy()
end
