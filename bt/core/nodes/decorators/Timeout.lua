
-----------------------------------------------------------------------------------------
-- Decorators
-- Timeout

-- Interupts decorated child node and returns Failure if the child node is still Running 
-- after the timeout period
-----------------------------------------------------------------------------------------

local Timeout = bt.Class("Timeout",bt.BTDecorator)
bt.Timeout = Timeout
    
function Timeout:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "Timeout"
    self.timeout = 1
    self.timer = 0
end

function Timeout:init(jsonData)
    if jsonData.timeout ~= nil then
        self.timeout = jsonData.timeout._value
    end
end

function Timeout:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Resting
    end
    self.status = decoratedConnection:execute(agent,blackboard)
    if self.status == bt.Status.Running then
        self.timer = self.timer + bt.deltaTime
    end
    if self.timer < self.timeout then
        return self.status
    end
    self.timer = 0
    decoratedConnection:reset(true)
    return bt.Status.Failure
end

function Timeout:onReset()
    self.timer = 0
end

function Timeout:destroy()
end
