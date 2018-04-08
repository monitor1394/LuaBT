
-----------------------------------------------------------------------------------------
-- Decorators
-- Remapper

-- Remap the child node's status to another status. Used to either invert the child's 
-- return status or to always return a specific status.
-----------------------------------------------------------------------------------------

local Remapper = bt.Class("Remapper",bt.BTDecorator)
bt.Remapper = Remapper
    
RemapStatus = 
{
    Failure = 0,
    Success = 1,
}

function Remapper:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "Remapper"
    self.successRemap = RemapStatus.Success
    self.failureRemap = RemapStatus.Failure
end

function Remapper:init(jsonData)
    if jsonData.successRemap ~= nil then
        self.successRemap = RemapStatus[jsonData.successRemap]
    end
    if jsonData.failureRemap ~= nil then
        self.failureRemap = RemapStatus[jsonData.failureRemap]
    end
end

function Remapper:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Resting
    end
    self.status = decoratedConnection:execute(agent,blackboard)
    if self.status == bt.Status.Success then
        return successRemap
    elseif self.status == bt.Status.Failure then
        return failureRemap
    end
    return self.status
end

function Remapper:destroy()
end
