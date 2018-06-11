
-----------------------------------------------------------------------------------------
-- Decorators
-- Inverter

-- Inverts Success to Failure and Failure to Success
-----------------------------------------------------------------------------------------

local Inverter = bt.Class("Inverter",bt.BTDecorator)
bt.Inverter = Inverter
    
function Inverter:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "Inverter"
end

function Inverter:init(jsonData)
end

function Inverter:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Resting
    end
    self.status = decoratedConnection:execute(agent,blackboard)
    if self.status == bt.Status.Success then
        return bt.Status.Failure
    elseif self.status == bt.Status.Failure then
        return bt.Status.Success
    end
    return self.status
end

function Inverter:destroy()
end
