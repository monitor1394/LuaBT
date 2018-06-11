
-----------------------------------------------------------------------------------------
-- Decorators
-- Setter

-- Set another Agent for the rest of the Tree dynamicaly from this point and on. All nodes 
-- under this will be executed for the new agent
-----------------------------------------------------------------------------------------

local Setter = bt.Class("Setter",bt.BTDecorator)
bt.Setter = Setter
    
function Setter:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "Setter"
    self.newAgent = nil
end

function Setter:init(jsonData)
    if jsonData.newAgent ~= nil then
        self.newAgent = jsonData.newAgent._value
    end
end

function Setter:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Setter
    end
    if self.newAgent ~= nil then
        agent = self.newAgent
    end
    return decoratedConnection:execute(agent,blackboard)
end

function Setter:destroy()
end
